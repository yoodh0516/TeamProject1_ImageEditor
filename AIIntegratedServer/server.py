from __future__ import annotations

import base64
import hashlib
import json
import logging
import os
import shutil
import threading
import time
import urllib.error
import urllib.parse
import urllib.request
from io import BytesIO
from pathlib import Path
from typing import Final

# PyTorch/OpenCV가 로드되기 전에 CPU 실행 환경을 먼저 고정합니다.
os.environ["KMP_DUPLICATE_LIB_OK"] = "True"
os.environ["CUDA_VISIBLE_DEVICES"] = "-1"

import cv2
import numpy as np
import requests
from fastapi import FastAPI, File, Form, HTTPException, UploadFile
from fastapi.responses import JSONResponse, Response
from PIL import Image, ImageOps
from starlette.concurrency import run_in_threadpool


SERVER_DIR: Final = Path(__file__).resolve().parent


def _windows_short_path(path: Path) -> Path | None:
    """Windows 네이티브 라이브러리가 읽을 수 있는 ASCII 짧은 경로를 구합니다."""
    if os.name != "nt":
        return path

    try:
        import ctypes

        get_short_path = ctypes.windll.kernel32.GetShortPathNameW
        get_short_path.argtypes = [
            ctypes.c_wchar_p,
            ctypes.c_wchar_p,
            ctypes.c_uint,
        ]
        get_short_path.restype = ctypes.c_uint
        required = get_short_path(str(path), None, 0)
        if required <= 0:
            return None
        buffer = ctypes.create_unicode_buffer(required)
        if get_short_path(str(path), buffer, required) <= 0:
            return None
        short_path = Path(buffer.value)
        return short_path if str(short_path).isascii() else None
    except (AttributeError, OSError, ValueError):
        return None


def _select_app_data_dir() -> Path:
    """한글 프로젝트 경로와 분리된 쓰기 가능한 AI 데이터 폴더를 선택합니다."""
    configured = os.getenv("IMAGEEDITOR_AI_HOME", "").strip()
    candidates: list[Path] = []
    if configured:
        candidates.append(Path(configured))

    if os.name == "nt":
        for variable in ("LOCALAPPDATA", "PUBLIC", "PROGRAMDATA", "TEMP"):
            value = os.getenv(variable, "").strip()
            if value:
                candidates.append(Path(value) / "ImageEditorAI")
        system_drive = os.getenv("SystemDrive", "C:").strip() or "C:"
        candidates.append(Path(f"{system_drive}\\ImageEditorAI"))
    else:
        candidates.append(SERVER_DIR / ".runtime")

    for candidate in candidates:
        try:
            candidate.mkdir(parents=True, exist_ok=True)
            native_candidate = candidate
            if os.name == "nt" and not str(candidate.resolve()).isascii():
                native_candidate = _windows_short_path(candidate)
                if native_candidate is None:
                    continue

            write_test = native_candidate / ".write_test"
            write_test.write_bytes(b"ok")
            write_test.unlink()
            return native_candidate
        except OSError:
            continue

    raise RuntimeError(
        "AI 모델을 저장할 영문 경로를 만들지 못했습니다. "
        "Windows 환경변수 IMAGEEDITOR_AI_HOME에 영문 폴더를 지정해 주세요."
    )


def _select_config_dir(fallback: Path) -> Path:
    """외부 API 키는 가능하면 Windows 사용자 전용 폴더에 보관합니다."""
    configured = os.getenv("IMAGEEDITOR_AI_HOME", "").strip()
    candidates: list[Path] = []
    if configured:
        candidates.append(Path(configured))

    if os.name == "nt":
        for variable in ("LOCALAPPDATA", "APPDATA"):
            value = os.getenv(variable, "").strip()
            if value:
                candidates.append(Path(value) / "ImageEditorAI")
    else:
        candidates.append(SERVER_DIR / ".runtime")

    candidates.append(fallback)
    for candidate in candidates:
        try:
            candidate.mkdir(parents=True, exist_ok=True)
            write_test = candidate / ".config_write_test"
            write_test.write_bytes(b"ok")
            write_test.unlink()
            return candidate
        except OSError:
            continue

    raise RuntimeError("외부 API 키를 저장할 사용자 설정 폴더를 만들지 못했습니다.")


APP_DATA_DIR: Final = _select_app_data_dir()
CONFIG_DIR: Final = _select_config_dir(APP_DATA_DIR)
MODEL_DIR: Final = APP_DATA_DIR / "models"
GEMINI_KEY_FILE: Final = CONFIG_DIR / "gemini_api_key.txt"
REPLICATE_TOKEN_FILE: Final = CONFIG_DIR / "replicate_api_token.txt"
MAX_UPLOAD_BYTES: Final = 25 * 1024 * 1024
DETAIL_MODEL_MAX_SIDE: Final = 1200
GEMINI_MODEL: Final = os.getenv("GEMINI_MODEL", "gemini-3.5-flash").strip()
REPLICATE_MODEL: Final = "flux-kontext-apps/restore-image"
REPLICATE_PREDICTION_URL: Final = (
    "https://api.replicate.com/v1/models/"
    f"{REPLICATE_MODEL}/predictions"
)
REPLICATE_MAX_INPUT_BYTES: Final = 950 * 1024
REPLICATE_MAX_OUTPUT_BYTES: Final = 50 * 1024 * 1024
REPLICATE_MAX_WAIT_SECONDS: Final = 6 * 60

MODEL_DIR.mkdir(parents=True, exist_ok=True)
# PyTorch/OpenCV의 Windows 한글 경로 오류를 피하도록 모델 작업 폴더를 고정합니다.
os.chdir(MODEL_DIR)

logging.basicConfig(
    filename=SERVER_DIR / "server.log",
    level=logging.INFO,
    format="%(asctime)s %(levelname)s %(message)s",
    encoding="utf-8",
)
logger = logging.getLogger("imageeditor-ai")

app = FastAPI(
    title="ImageEditor Integrated AI Server",
    description="ImageEditor의 로컬 CPU AI와 Replicate 사진 복원을 제공하는 단일 서버",
    version="4.3.0-python311-face-filter",
)

_background_session = None
_colorization_net = None
_detail_net = None
_sam_model = None
_lama_model = None
_face_detector = None
_background_lock = threading.Lock()
_colorization_lock = threading.Lock()
_detail_lock = threading.Lock()
_sam_load_lock = threading.Lock()
_lama_load_lock = threading.Lock()
_sam_inference_lock = threading.Lock()
_lama_inference_lock = threading.Lock()
_background_inference_lock = threading.Lock()
_restore_inference_lock = threading.Lock()
_face_detector_load_lock = threading.Lock()
_face_detector_inference_lock = threading.Lock()


class ModelDownloadError(RuntimeError):
    pass


class ReplicateServiceError(RuntimeError):
    pass


MODEL_FILES: Final = {
    "u2netp.onnx": {
        "sha256": "309c8469258dda742793dce0ebea8e6dd393174f89934733ecc8b14c76f4ddd8",
        "urls": [
            "https://github.com/danielgatis/rembg/releases/download/v0.0.0/"
            "u2netp.onnx",
            "https://huggingface.co/edgetools/u2netp/resolve/main/"
            "u2netp.onnx?download=true",
        ],
    },
    "big-lama.pt": {
        "sha256": "7ba7aa7ac37a4d41fdbbeba3a2af7ead18058552997e3a3cd1a3b2210c9e6b4c",
        "urls": [
            "https://huggingface.co/fashn-ai/LaMa/resolve/main/"
            "big-lama.pt?download=true",
        ],
    },
    "colorization_deploy_v2.prototxt": {
        "sha256": "d16418cef8df4ccd703a55ae0ef3960861d5010418f77c90d0a47689998a7169",
        "urls": [
            "https://raw.githubusercontent.com/richzhang/colorization/caffe/"
            "colorization/models/colorization_deploy_v2.prototxt",
        ],
    },
    "pts_in_hull.npy": {
        "sha256": "b5dec01315c34f43f1c8c089e84c45ae35d1838d8e77ed0e7ca930f79ffa450e",
        "urls": [
            "https://raw.githubusercontent.com/richzhang/colorization/caffe/"
            "colorization/resources/pts_in_hull.npy",
        ],
    },
    "colorization_release_v2.caffemodel": {
        "sha256": "f5af1e602646328c792e1094f9876fe9cd4c09ac46fa886e5708a1abc89137b1",
        "urls": [
            "https://huggingface.co/spaces/BilalSardar/Black-N-White-To-Color/"
            "resolve/main/colorization_release_v2.caffemodel?download=true",
            "https://www.dropbox.com/s/dx0qvhhp5hbcx7z/"
            "colorization_release_v2.caffemodel?dl=1",
        ],
    },
    "FSRCNN_x2.pb": {
        "sha256": "366b33f0084c7b3f2bf6724f0a2c77bca94fcec9d7b6d72389d330073b380d5c",
        "urls": [
            "https://raw.githubusercontent.com/Saafke/FSRCNN_Tensorflow/"
            "master/models/FSRCNN_x2.pb",
        ],
    },
    "blaze_face_full_range.tflite": {
        "sha256": "3698b18f063835bc609069ef052228fbe86d9c9a6dc8dcb7c7c2d69aed2b181b",
        "urls": [
            "https://storage.googleapis.com/mediapipe-models/face_detector/"
            "blaze_face_full_range/float16/1/blaze_face_full_range.tflite",
        ],
    },
}


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as file_handle:
        for chunk in iter(lambda: file_handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _download_model(model_name: str) -> Path:
    spec = MODEL_FILES[model_name]
    destination = MODEL_DIR / model_name

    # 이전 수정본에서 이미 받은 모델은 검증한 뒤 영문 캐시로 자동 복사합니다.
    legacy_path = SERVER_DIR / "models" / model_name
    if not destination.is_file() and legacy_path.is_file():
        try:
            if _sha256(legacy_path) == spec["sha256"]:
                shutil.copy2(legacy_path, destination)
        except OSError as exc:
            logger.warning("Legacy model migration failed (%s): %s", model_name, exc)

    if destination.is_file() and _sha256(destination) == spec["sha256"]:
        return destination

    if destination.exists():
        destination.unlink()

    temporary = destination.with_suffix(destination.suffix + ".download")
    last_error: Exception | None = None

    for url in spec["urls"]:
        try:
            logger.info("Downloading model file: %s", model_name)
            request = urllib.request.Request(
                url,
                headers={"User-Agent": "ImageEditor-Local-AI/1.0"},
            )
            with urllib.request.urlopen(request, timeout=90) as source:
                with temporary.open("wb") as target:
                    while True:
                        chunk = source.read(1024 * 1024)
                        if not chunk:
                            break
                        target.write(chunk)

            if _sha256(temporary) != spec["sha256"]:
                raise ModelDownloadError(
                    f"{model_name} 파일의 무결성 검사에 실패했습니다."
                )

            temporary.replace(destination)
            return destination
        except (OSError, urllib.error.URLError, ModelDownloadError) as exc:
            last_error = exc
            temporary.unlink(missing_ok=True)
            logger.warning("Model download failed (%s): %s", model_name, exc)

    raise ModelDownloadError(
        "AI 모델을 내려받지 못했습니다. 인터넷 연결을 확인한 뒤 "
        "같은 AI 기능을 다시 실행해 주세요."
    ) from last_error


async def _read_uploaded_image(file: UploadFile) -> bytes:
    if not file.content_type or not file.content_type.startswith("image/"):
        raise HTTPException(status_code=400, detail="이미지 파일만 업로드할 수 있습니다.")

    data = await file.read(MAX_UPLOAD_BYTES + 1)
    if not data:
        raise HTTPException(status_code=400, detail="빈 이미지 파일입니다.")
    if len(data) > MAX_UPLOAD_BYTES:
        raise HTTPException(status_code=413, detail="이미지 파일은 25MB 이하여야 합니다.")
    return data


def _prepare_torch_for_cpu():
    """모든 PyTorch 체크포인트를 현재 PC의 CPU로 읽도록 고정합니다."""
    import torch

    if getattr(torch, "_imageeditor_cpu_load_patch", False):
        return torch

    original_load = torch.load
    original_jit_load = torch.jit.load

    def cpu_load(*args, **kwargs):
        positional = list(args)
        if len(positional) >= 2:
            positional[1] = "cpu"
        else:
            kwargs["map_location"] = "cpu"
        return original_load(*positional, **kwargs)

    def cpu_jit_load(*args, **kwargs):
        positional = list(args)
        if len(positional) >= 2:
            positional[1] = "cpu"
        else:
            kwargs["map_location"] = "cpu"
        return original_jit_load(*positional, **kwargs)

    torch.load = cpu_load
    torch.jit.load = cpu_jit_load
    torch._imageeditor_cpu_load_patch = True
    return torch


class LocalSimpleLama:
    """Python 3.11에서 외부 래퍼 없이 LaMa TorchScript를 CPU로 실행합니다."""

    def __init__(self) -> None:
        torch = _prepare_torch_for_cpu()
        model_path = _download_model("big-lama.pt")
        self._torch = torch
        self._model = torch.jit.load(str(model_path), map_location="cpu")
        self._model.eval()
        self._model.to("cpu")

    def __call__(self, image: Image.Image, mask: Image.Image) -> Image.Image:
        rgb = np.asarray(image.convert("RGB"), dtype=np.float32) / 255.0
        mask_array = np.asarray(mask.convert("L"), dtype=np.uint8)
        height, width = rgb.shape[:2]

        # LaMa 입력 크기는 8의 배수여야 합니다. 대칭 패딩 후 원래 크기로 자릅니다.
        pad_height = (-height) % 8
        pad_width = (-width) % 8
        padded_rgb = np.pad(
            rgb,
            ((0, pad_height), (0, pad_width), (0, 0)),
            mode="symmetric",
        )
        padded_mask = np.pad(
            mask_array,
            ((0, pad_height), (0, pad_width)),
            mode="symmetric",
        )

        image_chw = np.ascontiguousarray(padded_rgb.transpose(2, 0, 1))
        binary_mask = np.ascontiguousarray((padded_mask > 0).astype(np.float32))
        image_tensor = self._torch.from_numpy(image_chw).unsqueeze(0).to("cpu")
        mask_tensor = self._torch.from_numpy(binary_mask).unsqueeze(0).unsqueeze(0)

        with self._torch.inference_mode():
            inpainted = self._model(image_tensor, mask_tensor)

        result = (
            inpainted[0, :, :height, :width]
            .permute(1, 2, 0)
            .detach()
            .cpu()
            .numpy()
        )
        result = np.clip(result * 255.0, 0, 255).astype(np.uint8)
        return Image.fromarray(result)


def _get_sam_model():
    global _sam_model
    if _sam_model is None:
        with _sam_load_lock:
            if _sam_model is None:
                _prepare_torch_for_cpu()
                from ultralytics import SAM

                model_path = MODEL_DIR / "mobile_sam.pt"
                legacy_paths = (
                    SERVER_DIR / "mobile_sam.pt",
                    SERVER_DIR / "models" / "mobile_sam.pt",
                )
                if not model_path.is_file():
                    for legacy_path in legacy_paths:
                        if not legacy_path.is_file():
                            continue
                        try:
                            shutil.copy2(legacy_path, model_path)
                            break
                        except OSError as exc:
                            logger.warning("MobileSAM migration failed: %s", exc)

                # 영문 캐시 폴더에 없으면 Ultralytics가 최초 1회 내려받습니다.
                _sam_model = SAM("mobile_sam.pt")
    return _sam_model


def _get_lama_model():
    global _lama_model
    if _lama_model is None:
        with _lama_load_lock:
            if _lama_model is None:
                _lama_model = LocalSimpleLama()
    return _lama_model


def _get_sam_mask(
    rgb: np.ndarray,
    mode: str,
    x: int,
    y: int,
    x1: int,
    y1: int,
    x2: int,
    y2: int,
) -> np.ndarray:
    height, width = rgb.shape[:2]
    normalized_mode = mode.strip().upper()

    if normalized_mode == "POINT":
        if not (0 <= x < width and 0 <= y < height):
            raise ValueError("선택한 점이 이미지 영역을 벗어났습니다.")
        prediction_options = {"points": [[x, y]], "labels": [1]}
    elif normalized_mode == "BOX":
        left, right = sorted((x1, x2))
        top, bottom = sorted((y1, y2))
        if left < 0 or top < 0 or right >= width or bottom >= height:
            raise ValueError("선택한 사각형이 이미지 영역을 벗어났습니다.")
        if right - left < 2 or bottom - top < 2:
            raise ValueError("선택 영역이 너무 작습니다.")
        prediction_options = {"bboxes": [[left, top, right, bottom]]}
    else:
        raise ValueError("mode는 POINT 또는 BOX여야 합니다.")

    with _sam_inference_lock:
        results = _get_sam_model().predict(
            rgb,
            device="cpu",
            verbose=False,
            **prediction_options,
        )

    if not results or results[0].masks is None:
        raise ValueError("선택한 영역에서 지울 물체를 찾지 못했습니다.")

    mask_tensor = results[0].masks.data
    if len(mask_tensor) == 0:
        raise ValueError("선택한 영역에서 지울 물체를 찾지 못했습니다.")

    mask = mask_tensor[0].detach().cpu().numpy()
    mask = np.where(mask >= 0.5, 255, 0).astype(np.uint8)
    return cv2.resize(mask, (width, height), interpolation=cv2.INTER_NEAREST)


def _preview_object_mask(
    bgr: np.ndarray,
    mask: np.ndarray,
) -> bytes:
    overlay = bgr.copy()
    overlay[mask > 0] = (255, 0, 255)
    preview = cv2.addWeighted(bgr, 0.6, overlay, 0.4, 0)
    return _encode_png(preview)


def _remove_object_with_lama(rgb: np.ndarray, mask: np.ndarray) -> bytes:
    kernel = np.ones((7, 7), dtype=np.uint8)
    dilated_mask = cv2.dilate(mask, kernel, iterations=1)

    with _lama_inference_lock:
        result = _get_lama_model()(
            Image.fromarray(rgb),
            Image.fromarray(dilated_mask),
        )

    if not isinstance(result, Image.Image):
        raise RuntimeError("LaMa 모델이 결과 이미지를 반환하지 않았습니다.")

    output = BytesIO()
    result.convert("RGB").save(output, format="PNG", optimize=True)
    return output.getvalue()


def _get_background_session():
    global _background_session
    if _background_session is None:
        with _background_lock:
            if _background_session is None:
                import onnxruntime as ort

                # rembg의 Python 버전 제한을 피하고 같은 U2NetP 모델을 직접 실행합니다.
                ort.set_default_logger_severity(3)
                options = ort.SessionOptions()
                options.graph_optimization_level = (
                    ort.GraphOptimizationLevel.ORT_ENABLE_ALL
                )
                options.inter_op_num_threads = 1
                options.intra_op_num_threads = max(1, min(4, os.cpu_count() or 1))
                _background_session = ort.InferenceSession(
                    str(_download_model("u2netp.onnx")),
                    sess_options=options,
                    providers=["CPUExecutionProvider"],
                )
    return _background_session


def _remove_background_with_u2netp(image: Image.Image) -> Image.Image:
    session = _get_background_session()
    resampling = Image.Resampling.LANCZOS
    model_image = image.convert("RGB").resize((320, 320), resampling)
    image_array = np.asarray(model_image, dtype=np.float32)
    image_array /= max(float(np.max(image_array)), 1e-6)
    image_array = (
        image_array - np.asarray([0.485, 0.456, 0.406], dtype=np.float32)
    ) / np.asarray([0.229, 0.224, 0.225], dtype=np.float32)
    model_input = np.ascontiguousarray(
        image_array.transpose(2, 0, 1)[np.newaxis, ...],
        dtype=np.float32,
    )

    with _background_inference_lock:
        outputs = session.run(
            None,
            {session.get_inputs()[0].name: model_input},
        )
    if not outputs:
        raise RuntimeError("U2NetP 모델이 배경 마스크를 반환하지 않았습니다.")

    prediction = np.asarray(outputs[0], dtype=np.float32)[0, 0]
    minimum = float(np.min(prediction))
    maximum = float(np.max(prediction))
    if maximum - minimum < 1e-6:
        mask_small = np.zeros(prediction.shape, dtype=np.uint8)
    else:
        normalized = (prediction - minimum) / (maximum - minimum)
        mask_small = np.clip(normalized * 255.0, 0, 255).astype(np.uint8)

    mask_image = Image.fromarray(mask_small).resize(image.size, resampling)
    mask = np.asarray(mask_image, dtype=np.uint8)
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
    mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, kernel)
    mask = cv2.GaussianBlur(mask, (5, 5), sigmaX=2, sigmaY=2)
    mask = np.where(mask < 127, 0, 255).astype(np.uint8)

    result = image.convert("RGBA")
    original_alpha = np.asarray(result.getchannel("A"), dtype=np.uint16)
    combined_alpha = (
        (original_alpha * mask.astype(np.uint16) + 127) // 255
    ).astype(np.uint8)
    result.putalpha(Image.fromarray(combined_alpha))
    return result


def _get_colorization_net():
    global _colorization_net
    if _colorization_net is None:
        with _colorization_lock:
            if _colorization_net is None:
                prototxt = _download_model("colorization_deploy_v2.prototxt")
                weights = _download_model("colorization_release_v2.caffemodel")
                points_path = _download_model("pts_in_hull.npy")

                net = cv2.dnn.readNetFromCaffe(str(prototxt), str(weights))
                points = np.load(points_path).transpose().reshape(2, 313, 1, 1)
                net.getLayer(net.getLayerId("class8_ab")).blobs = [
                    points.astype(np.float32)
                ]
                net.getLayer(net.getLayerId("conv8_313_rh")).blobs = [
                    np.full((1, 313), 2.606, dtype=np.float32)
                ]
                _colorization_net = net
    return _colorization_net


def _get_detail_net():
    global _detail_net
    if _detail_net is None:
        with _detail_lock:
            if _detail_net is None:
                model_path = _download_model("FSRCNN_x2.pb")
                net = cv2.dnn_superres.DnnSuperResImpl_create()
                net.readModel(str(model_path))
                net.setModel("fsrcnn", 2)
                _detail_net = net
    return _detail_net


def _decode_uploaded_image(data: bytes) -> tuple[Image.Image, np.ndarray]:
    try:
        pil_image = Image.open(BytesIO(data))
        pil_image = ImageOps.exif_transpose(pil_image).convert("RGB")
        rgb = np.asarray(pil_image, dtype=np.uint8)
        bgr = cv2.cvtColor(rgb, cv2.COLOR_RGB2BGR)
        return pil_image, bgr
    except (OSError, ValueError) as exc:
        raise ValueError("이미지 파일을 읽을 수 없습니다.") from exc


def _get_face_detector():
    """MediaPipe full-range detector를 한 번만 만들고 모든 요청에서 재사용합니다."""
    global _face_detector
    if _face_detector is None:
        with _face_detector_load_lock:
            if _face_detector is None:
                try:
                    import mediapipe as mp
                except ImportError as exc:
                    raise RuntimeError(
                        "MediaPipe가 설치되지 않았습니다. "
                        "AIIntegratedServer\\1_setup_server.bat를 다시 실행해 주세요."
                    ) from exc

                model_path = _download_model("blaze_face_full_range.tflite")
                options = mp.tasks.vision.FaceDetectorOptions(
                    base_options=mp.tasks.BaseOptions(
                        model_asset_path=str(model_path)
                    ),
                    running_mode=mp.tasks.vision.RunningMode.IMAGE,
                    min_detection_confidence=0.4,
                    min_suppression_threshold=0.3,
                )
                _face_detector = (
                    mp.tasks.vision.FaceDetector.create_from_options(options)
                )
    return _face_detector


def _detect_faces(rgb: np.ndarray) -> dict[str, list[dict[str, float]]]:
    """얼굴 필터 자동 배치에 필요한 원본 이미지 좌표를 반환합니다."""
    import mediapipe as mp

    height, width = rgb.shape[:2]
    contiguous_rgb = np.ascontiguousarray(rgb)
    media_image = mp.Image(
        image_format=mp.ImageFormat.SRGB,
        data=contiguous_rgb,
    )

    detector = _get_face_detector()
    with _face_detector_inference_lock:
        result = detector.detect(media_image)

    faces: list[dict[str, float]] = []
    for detection in result.detections:
        box = detection.bounding_box
        x = max(0.0, min(float(width - 1), float(box.origin_x)))
        y = max(0.0, min(float(height - 1), float(box.origin_y)))
        box_width = max(1.0, min(float(width) - x, float(box.width)))
        box_height = max(1.0, min(float(height) - y, float(box.height)))

        keypoints = detection.keypoints or []

        def keypoint(index: int) -> tuple[float, float]:
            if index >= len(keypoints):
                return x + box_width / 2.0, y + box_height / 2.0
            point = keypoints[index]
            return (
                max(0.0, min(float(width), float(point.x) * width)),
                max(0.0, min(float(height), float(point.y) * height)),
            )

        left_eye_x, left_eye_y = keypoint(0)
        right_eye_x, right_eye_y = keypoint(1)
        nose_x, nose_y = keypoint(2)
        eye_dx = right_eye_x - left_eye_x
        eye_dy = right_eye_y - left_eye_y
        rotation = float(np.degrees(np.arctan2(eye_dy, eye_dx)))
        if rotation > 90.0:
            rotation -= 180.0
        elif rotation < -90.0:
            rotation += 180.0

        confidence = 0.0
        if detection.categories:
            confidence = float(detection.categories[0].score or 0.0)

        faces.append(
            {
                "x": x,
                "y": y,
                "width": box_width,
                "height": box_height,
                "nose_x": nose_x,
                "nose_y": nose_y,
                "rotation": rotation,
                "confidence": confidence,
            }
        )

    # 현재 커뮤니티 편집기는 한 번에 하나의 오버레이를 미세 조정하므로
    # 가장 크게 보이는 얼굴이 항상 첫 번째가 되게 합니다.
    faces.sort(key=lambda face: face["width"] * face["height"], reverse=True)
    return {"faces": faces}


def _is_grayscale_or_faded(bgr: np.ndarray) -> bool:
    channel_range = np.max(bgr, axis=2) - np.min(bgr, axis=2)
    if float(np.mean(channel_range)) < 8.0:
        return True

    hsv = cv2.cvtColor(bgr, cv2.COLOR_BGR2HSV)
    saturation = hsv[:, :, 1]
    if (
        float(np.mean(saturation)) < 18.0
        and float(np.percentile(saturation, 90)) < 35.0
    ):
        return True

    # 세피아 사진은 RGB 값의 차이는 크지만 세 채널이 거의 같은 명암 형태로
    # 움직이고 Lab 색차 분산이 작습니다. 단순 채널 차이만 보면 컬러사진으로
    # 오판하므로 두 조건을 함께 사용해 단색으로 염색된 오래된 사진도 찾습니다.
    lab = cv2.cvtColor(bgr, cv2.COLOR_BGR2LAB)
    chroma_std = np.std(lab[:, :, 1:].astype(np.float32), axis=(0, 1))
    sampled = bgr[::4, ::4].reshape(-1, 3).astype(np.float32)
    channel_std = np.std(sampled, axis=0)
    if sampled.shape[0] < 4 or float(np.min(channel_std)) < 1.0:
        return False

    correlation = np.corrcoef(sampled, rowvar=False)
    channel_pairs = correlation[np.triu_indices(3, 1)]
    minimum_correlation = float(np.nanmin(channel_pairs))
    sepia_monochrome = (
        float(np.max(chroma_std)) < 7.0
        and minimum_correlation > 0.96
        and float(np.mean(saturation)) < 115.0
    )
    return sepia_monochrome


def _protect_detected_faces(mask: np.ndarray, gray: np.ndarray) -> None:
    """자동 균열 마스크가 얼굴과 머리 장식을 바꾸지 않도록 보호합니다."""
    try:
        cascade_path = cv2.data.haarcascades + "haarcascade_frontalface_default.xml"
        detector = cv2.CascadeClassifier(cascade_path)
        if detector.empty():
            return

        height, width = gray.shape
        max_face = max(40, min(width, height) // 3)
        faces = detector.detectMultiScale(
            gray,
            scaleFactor=1.08,
            minNeighbors=5,
            minSize=(24, 24),
            maxSize=(max_face, max_face),
        )
        for x, y, face_width, face_height in faces:
            horizontal_padding = max(5, round(face_width * 0.30))
            top_padding = max(6, round(face_height * 0.55))
            bottom_padding = max(5, round(face_height * 0.35))
            left = max(0, x - horizontal_padding)
            top = max(0, y - top_padding)
            right = min(width - 1, x + face_width + horizontal_padding)
            bottom = min(height - 1, y + face_height + bottom_padding)
            cv2.rectangle(mask, (left, top), (right, bottom), 0, thickness=-1)
    except (AttributeError, cv2.error, OSError):
        # 얼굴 검출은 보호 장치일 뿐이므로 검출기 문제로 전체 복원을 중단하지 않습니다.
        return


def _select_scratch_components(
    binary: np.ndarray,
    longest_side: int,
    *,
    strict: bool,
) -> np.ndarray:
    """점 형태의 질감은 버리고 길고 가는 균열 성분만 남깁니다."""
    component_count, labels, stats, _ = cv2.connectedComponentsWithStats(
        binary,
        connectivity=8,
    )
    selected = np.zeros_like(binary)
    minimum_diagonal = max(7.0, longest_side * (0.020 if strict else 0.015))
    maximum_area = max(32, round(binary.size * 0.0025))

    for label in range(1, component_count):
        _, _, width, height, area = stats[label]
        if area < 3 or area > maximum_area:
            continue

        diagonal = float(np.hypot(width, height))
        aspect_ratio = max(width, height) / max(1, min(width, height))
        fill_ratio = area / max(1, width * height)

        if strict:
            looks_like_scratch = (
                aspect_ratio >= 3.2 and diagonal >= minimum_diagonal
            ) or (
                fill_ratio <= 0.14 and diagonal >= minimum_diagonal * 2.0
            )
        else:
            looks_like_scratch = (
                aspect_ratio >= 2.5 and diagonal >= minimum_diagonal
            ) or (
                fill_ratio <= 0.24 and diagonal >= minimum_diagonal * 1.8
            )

        if looks_like_scratch:
            selected[labels == label] = 255

    return selected


def _build_scratch_mask(gray: np.ndarray) -> np.ndarray:
    """세피아 사진의 밝은 균열과 일부 짙은 긁힘을 고신뢰도로 찾습니다."""
    height, width = gray.shape
    longest_side = max(height, width)
    median_size = 7 if min(height, width) >= 7 else 3
    local_median = cv2.medianBlur(gray, median_size)
    bright_residual = cv2.subtract(gray, local_median)
    dark_residual = cv2.subtract(local_median, gray)

    bright_threshold = max(12.0, float(np.percentile(bright_residual, 96.5)))
    dark_threshold = max(22.0, float(np.percentile(dark_residual, 99.0)))
    bright = np.where(bright_residual >= bright_threshold, 255, 0).astype(np.uint8)
    dark = np.where(dark_residual >= dark_threshold, 255, 0).astype(np.uint8)

    close_kernel = cv2.getStructuringElement(cv2.MORPH_CROSS, (3, 3))
    bright = cv2.morphologyEx(bright, cv2.MORPH_CLOSE, close_kernel)
    dark = cv2.morphologyEx(dark, cv2.MORPH_CLOSE, close_kernel)
    mask = cv2.bitwise_or(
        _select_scratch_components(bright, longest_side, strict=False),
        _select_scratch_components(dark, longest_side, strict=True),
    )

    _protect_detected_faces(mask, gray)

    # 액자 테두리와 사진 하단의 촬영 문구·날짜는 손상으로 오인하기 쉽습니다.
    border = max(4, round(min(height, width) * 0.015))
    caption_height = max(border, round(height * 0.065))
    mask[:border, :] = 0
    mask[-caption_height:, :] = 0
    mask[:, :border] = 0
    mask[:, -border:] = 0

    dilation_radius = max(1, min(3, round(longest_side / 700)))
    dilation_size = dilation_radius * 2 + 1
    dilation_kernel = cv2.getStructuringElement(
        cv2.MORPH_ELLIPSE,
        (dilation_size, dilation_size),
    )
    mask = cv2.dilate(mask, dilation_kernel, iterations=1)
    if cv2.countNonZero(mask) > mask.size * 0.06:
        # 무늬가 매우 복잡한 사진을 균열로 과검출한 경우 원본 훼손을 우선 막습니다.
        return np.zeros_like(mask)
    return mask


def _repair_damage_and_fading(bgr: np.ndarray) -> np.ndarray:
    """Scratch, noise and faded-contrast restoration for old photos."""
    height, width = bgr.shape[:2]
    longest_side = max(height, width)
    scale = min(1.0, 1800.0 / longest_side)

    if scale < 1.0:
        working = cv2.resize(
            bgr,
            (max(1, round(width * scale)), max(1, round(height * scale))),
            interpolation=cv2.INTER_AREA,
        )
    else:
        working = bgr.copy()

    denoised = cv2.fastNlMeansDenoisingColored(
        working,
        None,
        h=3,
        hColor=3,
        templateWindowSize=7,
        searchWindowSize=21,
    )

    gray = cv2.cvtColor(denoised, cv2.COLOR_BGR2GRAY)
    scratch_mask = _build_scratch_mask(gray)
    if cv2.countNonZero(scratch_mask) > 0:
        inpaint_radius = max(2, min(4, round(max(gray.shape) / 600)))
        denoised = cv2.inpaint(
            denoised,
            scratch_mask,
            inpaint_radius,
            cv2.INPAINT_TELEA,
        )

    lab = cv2.cvtColor(denoised, cv2.COLOR_BGR2LAB)
    lightness, channel_a, channel_b = cv2.split(lab)
    enhanced_lightness = cv2.createCLAHE(clipLimit=1.5, tileGridSize=(8, 8)).apply(
        lightness
    )
    lightness = cv2.addWeighted(lightness, 0.65, enhanced_lightness, 0.35, 0)
    restored = cv2.cvtColor(
        cv2.merge((lightness, channel_a, channel_b)),
        cv2.COLOR_LAB2BGR,
    )

    if scale < 1.0:
        restored = cv2.resize(restored, (width, height), interpolation=cv2.INTER_CUBIC)
    return restored


def _colorize_with_ai(bgr: np.ndarray) -> np.ndarray:
    net = _get_colorization_net()
    scaled = bgr.astype(np.float32) / 255.0
    lab = cv2.cvtColor(scaled, cv2.COLOR_BGR2LAB)
    resized = cv2.resize(lab, (224, 224), interpolation=cv2.INTER_AREA)
    lightness = resized[:, :, 0] - 50.0

    net.setInput(cv2.dnn.blobFromImage(lightness))
    predicted_ab = net.forward()[0, :, :, :].transpose((1, 2, 0))
    predicted_ab = cv2.resize(
        predicted_ab,
        (bgr.shape[1], bgr.shape[0]),
        interpolation=cv2.INTER_CUBIC,
    )

    original_lightness = lab[:, :, 0]
    colorized_lab = np.concatenate(
        (original_lightness[:, :, np.newaxis], predicted_ab),
        axis=2,
    )
    colorized = cv2.cvtColor(colorized_lab, cv2.COLOR_LAB2BGR)
    return np.clip(colorized * 255.0, 0, 255).astype(np.uint8)


def _enhance_details_with_ai(bgr: np.ndarray) -> np.ndarray:
    height, width = bgr.shape[:2]
    longest_side = max(height, width)
    scale = min(1.0, DETAIL_MODEL_MAX_SIDE / longest_side)

    if scale < 1.0:
        model_input = cv2.resize(
            bgr,
            (max(1, round(width * scale)), max(1, round(height * scale))),
            interpolation=cv2.INTER_AREA,
        )
    else:
        model_input = bgr

    ai_upscaled = _get_detail_net().upsample(model_input)
    reconstructed = cv2.resize(
        ai_upscaled,
        (width, height),
        interpolation=cv2.INTER_AREA,
    )
    return cv2.addWeighted(bgr, 0.55, reconstructed, 0.45, 0)


def _restore_photo(
    bgr: np.ndarray,
    restore_damage: bool,
    colorize: bool,
    enhance_details: bool,
) -> tuple[np.ndarray, bool]:
    with _restore_inference_lock:
        result = _repair_damage_and_fading(bgr) if restore_damage else bgr.copy()
        was_colorized = colorize and _is_grayscale_or_faded(result)

        if was_colorized:
            result = _colorize_with_ai(result)

        if enhance_details:
            result = _enhance_details_with_ai(result)

        return result, was_colorized


def _encode_png(bgr: np.ndarray) -> bytes:
    success, encoded = cv2.imencode(
        ".png",
        bgr,
        [cv2.IMWRITE_PNG_COMPRESSION, 3],
    )
    if not success:
        raise RuntimeError("결과 이미지를 PNG로 만들지 못했습니다.")
    return encoded.tobytes()


def save_replicate_api_token(api_token: str) -> None:
    """Replicate 토큰을 프로젝트 밖의 사용자 전용 설정 폴더에 저장합니다."""
    normalized_token = api_token.strip()
    if not normalized_token:
        raise ValueError("Replicate API 토큰이 비어 있습니다.")
    if any(character.isspace() for character in normalized_token):
        raise ValueError("Replicate API 토큰에는 공백을 넣을 수 없습니다.")
    if not normalized_token.startswith("r8_") or len(normalized_token) < 20:
        raise ValueError("Replicate API 토큰은 r8_로 시작하는 값을 입력해 주세요.")

    temporary = REPLICATE_TOKEN_FILE.with_suffix(".tmp")
    temporary.write_text(normalized_token, encoding="utf-8")
    temporary.replace(REPLICATE_TOKEN_FILE)
    try:
        os.chmod(REPLICATE_TOKEN_FILE, 0o600)
    except OSError:
        pass


def _get_replicate_api_token() -> str:
    if REPLICATE_TOKEN_FILE.is_file():
        try:
            saved_token = REPLICATE_TOKEN_FILE.read_text(encoding="utf-8").strip()
            if saved_token:
                return saved_token
        except OSError as exc:
            logger.warning("Replicate token file read failed: %s", exc)
    return os.getenv("REPLICATE_API_TOKEN", "").strip()


def _prepare_replicate_input_data_uri(image: Image.Image) -> str:
    """HTTP API에 안전하게 넣을 수 있도록 이미지를 1MB 이하 JPEG로 만듭니다."""
    working = image.convert("RGB")
    longest_side = max(working.size)
    if longest_side > 1536:
        ratio = 1536.0 / longest_side
        working = working.resize(
            (
                max(1, round(working.width * ratio)),
                max(1, round(working.height * ratio)),
            ),
            Image.Resampling.LANCZOS,
        )

    encoded = b""
    for quality in (90, 84, 78, 72, 66):
        output = BytesIO()
        working.save(
            output,
            format="JPEG",
            quality=quality,
            optimize=True,
            progressive=True,
        )
        encoded = output.getvalue()
        if len(encoded) <= REPLICATE_MAX_INPUT_BYTES:
            break

    while len(encoded) > REPLICATE_MAX_INPUT_BYTES and max(working.size) > 640:
        working = working.resize(
            (
                max(1, round(working.width * 0.85)),
                max(1, round(working.height * 0.85)),
            ),
            Image.Resampling.LANCZOS,
        )
        output = BytesIO()
        working.save(output, format="JPEG", quality=70, optimize=True)
        encoded = output.getvalue()

    if not encoded or len(encoded) > REPLICATE_MAX_INPUT_BYTES:
        raise ReplicateServiceError("Replicate에 보낼 이미지 크기를 줄이지 못했습니다.")

    encoded_text = base64.b64encode(encoded).decode("ascii")
    return f"data:image/jpeg;base64,{encoded_text}"


def _extract_replicate_error(response: requests.Response) -> str:
    try:
        payload = response.json()
        if isinstance(payload, dict):
            for key in ("detail", "error", "title"):
                value = payload.get(key)
                if value:
                    return str(value)[:500]
    except (ValueError, AttributeError):
        pass

    text = response.text.strip()
    return text[:500] if text else f"HTTP {response.status_code}"


def _extract_replicate_output_url(output) -> str:
    if isinstance(output, str):
        if output.startswith(("https://", "data:image/")):
            return output
        return ""
    if isinstance(output, list):
        for item in output:
            result = _extract_replicate_output_url(item)
            if result:
                return result
        return ""
    if isinstance(output, dict):
        for key in ("url", "image", "output"):
            result = _extract_replicate_output_url(output.get(key))
            if result:
                return result
    return ""


def _read_replicate_output(output_url: str, headers: dict[str, str]) -> bytes:
    if output_url.startswith("data:image/"):
        try:
            metadata, encoded = output_url.split(",", 1)
            if ";base64" not in metadata:
                raise ValueError
            result = base64.b64decode(encoded, validate=True)
        except (ValueError, TypeError) as exc:
            raise ReplicateServiceError("Replicate 결과 이미지 형식이 잘못되었습니다.") from exc
    else:
        parsed = urllib.parse.urlparse(output_url)
        host = (parsed.hostname or "").lower()
        if parsed.scheme != "https" or not (
            host == "replicate.delivery" or host.endswith(".replicate.delivery")
        ):
            raise ReplicateServiceError("Replicate가 허용되지 않은 결과 주소를 반환했습니다.")

        response = requests.get(output_url, headers=headers, timeout=(10, 90))
        if not response.ok:
            raise ReplicateServiceError(
                "Replicate 결과 이미지를 내려받지 못했습니다: "
                + _extract_replicate_error(response)
            )
        result = response.content

    if not result:
        raise ReplicateServiceError("Replicate 결과 이미지가 비어 있습니다.")
    if len(result) > REPLICATE_MAX_OUTPUT_BYTES:
        raise ReplicateServiceError("Replicate 결과 이미지가 50MB를 초과했습니다.")
    return result


def _normalize_replicate_output(image_bytes: bytes, target_size: tuple[int, int]) -> bytes:
    """복원 결과를 원본 크기의 표준 PNG로 바꿔 편집 캔버스 크기를 보존합니다."""
    try:
        image = Image.open(BytesIO(image_bytes))
        image = ImageOps.exif_transpose(image).convert("RGB")
    except (OSError, ValueError) as exc:
        raise ReplicateServiceError("Replicate 결과를 이미지로 읽을 수 없습니다.") from exc

    if image.size != target_size:
        image = image.resize(target_size, Image.Resampling.LANCZOS)

    output = BytesIO()
    image.save(output, format="PNG", optimize=True)
    return output.getvalue()


def _request_replicate_photo_restoration(
    image: Image.Image,
    api_token: str,
) -> bytes:
    """Replicate의 FLUX Kontext 사진 복원 모델을 실행하고 PNG를 반환합니다."""
    headers = {
        "Authorization": f"Bearer {api_token}",
        "Content-Type": "application/json",
        "Prefer": "wait=60",
        "Cancel-After": "6m",
    }
    payload = {
        "input": {
            "input_image": _prepare_replicate_input_data_uri(image),
            "output_format": "png",
        }
    }

    response = requests.post(
        REPLICATE_PREDICTION_URL,
        json=payload,
        headers=headers,
        timeout=(15, 75),
    )
    if not response.ok:
        raise ReplicateServiceError(_extract_replicate_error(response))

    try:
        prediction = response.json()
    except ValueError as exc:
        raise ReplicateServiceError("Replicate 응답을 해석할 수 없습니다.") from exc

    deadline = time.monotonic() + REPLICATE_MAX_WAIT_SECONDS
    poll_headers = {"Authorization": f"Bearer {api_token}"}
    while True:
        status = str(prediction.get("status", "")).lower()
        if status == "succeeded":
            break
        if status in {"failed", "canceled"}:
            message = prediction.get("error") or f"예측 상태: {status}"
            raise ReplicateServiceError(str(message)[:500])
        if status not in {"starting", "processing"}:
            raise ReplicateServiceError("Replicate가 알 수 없는 처리 상태를 반환했습니다.")
        if time.monotonic() >= deadline:
            raise ReplicateServiceError("Replicate 사진 복원 시간이 6분을 초과했습니다.")

        prediction_id = str(prediction.get("id", ""))
        if (
            not prediction_id
            or len(prediction_id) > 128
            or not all(character.isalnum() or character in "-_" for character in prediction_id)
        ):
            raise ReplicateServiceError("Replicate 예측 번호가 올바르지 않습니다.")

        time.sleep(2)
        poll_url = (
            "https://api.replicate.com/v1/predictions/"
            + urllib.parse.quote(prediction_id, safe="-_")
        )
        response = requests.get(poll_url, headers=poll_headers, timeout=(10, 30))
        if not response.ok:
            raise ReplicateServiceError(_extract_replicate_error(response))
        try:
            prediction = response.json()
        except ValueError as exc:
            raise ReplicateServiceError("Replicate 처리 상태를 해석할 수 없습니다.") from exc

    output_url = _extract_replicate_output_url(prediction.get("output"))
    if not output_url:
        raise ReplicateServiceError("Replicate가 복원 결과 주소를 반환하지 않았습니다.")

    result_bytes = _read_replicate_output(output_url, poll_headers)
    return _normalize_replicate_output(result_bytes, image.size)


def _extract_gemini_error(response: requests.Response) -> str:
    try:
        payload = response.json()
        message = payload.get("error", {}).get("message")
        if message:
            return str(message)
    except (ValueError, AttributeError):
        pass

    text = response.text.strip()
    return text[:500] if text else f"HTTP {response.status_code}"


def save_gemini_api_key(api_key: str) -> None:
    """Gemini 키를 프로젝트 밖의 사용자 전용 설정 폴더에 저장합니다."""
    normalized_key = api_key.strip()
    if not normalized_key:
        raise ValueError("Gemini API 키가 비어 있습니다.")
    if any(character.isspace() for character in normalized_key):
        raise ValueError("Gemini API 키에는 공백을 넣을 수 없습니다.")

    temporary = GEMINI_KEY_FILE.with_suffix(".tmp")
    temporary.write_text(normalized_key, encoding="utf-8")
    temporary.replace(GEMINI_KEY_FILE)
    try:
        os.chmod(GEMINI_KEY_FILE, 0o600)
    except OSError:
        pass


def _get_gemini_api_key() -> str:
    if GEMINI_KEY_FILE.is_file():
        try:
            saved_key = GEMINI_KEY_FILE.read_text(encoding="utf-8").strip()
            if saved_key:
                return saved_key
        except OSError as exc:
            logger.warning("Gemini key file read failed: %s", exc)
    return os.getenv("GEMINI_API_KEY", "").strip()


def _request_music_recommendation(
    image_bytes: bytes,
    mime_type: str,
    api_key: str,
) -> str:
    encoded_image = base64.b64encode(image_bytes).decode("ascii")
    model_name = urllib.parse.quote(GEMINI_MODEL, safe="-._")
    url = (
        "https://generativelanguage.googleapis.com/v1beta/models/"
        f"{model_name}:generateContent?key={urllib.parse.quote(api_key, safe='')}"
    )

    prompt = (
        "Analyze the atmosphere of this image and recommend exactly one song. "
        "Answer strictly in Korean using this format: Artist - Song Title. "
        "Do not add explanations, reasons, numbering, markdown, or extra text."
    )
    payload = {
        "contents": [
            {
                "parts": [
                    {"text": prompt},
                    {
                        "inline_data": {
                            "mime_type": mime_type,
                            "data": encoded_image,
                        }
                    },
                ]
            }
        ],
        "generationConfig": {
            	"maxOutputTokens": 512,
    		"thinkingConfig": {
        		"thinkingLevel": "MINIMAL",
        	},
	},
    }

    response = requests.post(url, json=payload, timeout=(10, 90))
    if not response.ok:
        raise RuntimeError(_extract_gemini_error(response))

    result = response.json()
    candidates = result.get("candidates") or []
    if not candidates:
        raise RuntimeError("Gemini가 추천 결과를 반환하지 않았습니다.")

    parts = candidates[0].get("content", {}).get("parts", [])
    recommendation = "".join(
        str(part.get("text", "")) for part in parts if isinstance(part, dict)
    ).strip()
    if not recommendation:
        raise RuntimeError("Gemini 추천 문장이 비어 있습니다.")
    return recommendation


def _request_caption_recommendations(
    image_bytes: bytes,
    mime_type: str,
    api_key: str,
) -> list[dict[str, str]]:
    """사진 위에 놓을 짧은 문구와 안전한 기본 위치를 Gemini에 요청합니다."""
    encoded_image = base64.b64encode(image_bytes).decode("ascii")
    model_name = urllib.parse.quote(GEMINI_MODEL, safe="-._")
    url = (
        "https://generativelanguage.googleapis.com/v1beta/models/"
        f"{model_name}:generateContent?key={urllib.parse.quote(api_key, safe='')}"
    )
    prompt = (
        "Analyze this image for a Korean SNS photo editor. Return exactly four "
        "Korean text overlay recommendations. Two should be short (3 to 12 Korean "
        "characters) and two can be emotional multi-line captions with up to two "
        "hashtags. Return JSON only, with this exact schema: "
        '{"captions":[{"text":"...","placement":"bottom-left|top-left",'
        '"effect":"none|shadow|gradient|outline|neon"}]}. '
        "Do not use markdown. Choose bottom-left unless the upper area is clearly "
        "darker and less busy, then choose top-left."
    )
    payload = {
        "contents": [
            {
                "parts": [
                    {"text": prompt},
                    {"inline_data": {"mime_type": mime_type, "data": encoded_image}},
                ]
            }
        ],
        "generationConfig": {
            "maxOutputTokens": 1024,
            "thinkingConfig": {"thinkingLevel": "MINIMAL"},
        },
    }
    response = requests.post(url, json=payload, timeout=(10, 90))
    if not response.ok:
        raise RuntimeError(_extract_gemini_error(response))

    candidates = response.json().get("candidates") or []
    if not candidates:
        raise RuntimeError("Gemini가 문구 추천 결과를 반환하지 않았습니다.")
    parts = candidates[0].get("content", {}).get("parts", [])
    raw_text = "".join(
        str(part.get("text", "")) for part in parts if isinstance(part, dict)
    ).strip()
    if raw_text.startswith("```"):
        raw_text = raw_text.split("\n", 1)[-1]
        if raw_text.rstrip().endswith("```"):
            raw_text = raw_text.rstrip()[:-3].rstrip()
    try:
        payload = json.loads(raw_text)
    except json.JSONDecodeError as exc:
        raise RuntimeError("Gemini 문구 추천 결과 형식이 올바르지 않습니다.") from exc

    allowed_placements = {"bottom-left", "top-left"}
    allowed_effects = {"none", "shadow", "gradient", "outline", "neon"}
    captions: list[dict[str, str]] = []
    for item in payload.get("captions", []):
        if not isinstance(item, dict):
            continue
        text = str(item.get("text", "")).strip()
        if not text:
            continue
        captions.append(
            {
                "text": text[:240],
                "placement": item.get("placement")
                if item.get("placement") in allowed_placements
                else "bottom-left",
                "effect": item.get("effect")
                if item.get("effect") in allowed_effects
                else "shadow",
            }
        )
        if len(captions) == 4:
            break
    if not captions:
        raise RuntimeError("사용할 수 있는 AI 문구 추천 결과가 없습니다.")
    return captions


def _request_social_post_recommendation(
    image_bytes: bytes,
    mime_type: str,
    api_key: str,
    platform: str,
    tone: str,
    length: str,
) -> dict[str, str]:
    """현재 사진에 맞는 SNS 게시글 두 문장과 해시태그를 생성합니다."""
    encoded_image = base64.b64encode(image_bytes).decode("ascii")
    model_name = urllib.parse.quote(GEMINI_MODEL, safe="-._")
    url = (
        "https://generativelanguage.googleapis.com/v1beta/models/"
        f"{model_name}:generateContent?key={urllib.parse.quote(api_key, safe='')}"
    )

    platform_rules = {
        "Instagram": "자연스럽고 감성적인 문장과 사진 중심의 해시태그를 사용하세요.",
        "Threads": "친근하고 대화하듯 자연스러운 문장으로 작성하세요.",
        "Facebook": "친구나 지인에게 공유하기 좋은 편안한 문장으로 작성하세요.",
        "X": "짧고 분명하며 핵심이 바로 보이는 문장으로 작성하세요.",
    }
    length_rules = {
        "짧게": "두 문장을 합쳐 35자 이내로 작성하세요.",
        "보통": "두 문장을 합쳐 55자 이내로 작성하세요.",
        "길게": "두 문장을 합쳐 80자 이내로 작성하세요.",
    }
    platform_rule = platform_rules.get(platform, platform_rules["Instagram"])
    length_rule = length_rules.get(length, length_rules["보통"])

    prompt = f"""
사진을 분석해 사용자가 SNS에 바로 복사할 수 있는 한국어 게시글과 해시태그를 만드세요.

플랫폼: {platform}
분위기: {tone}
길이: {length}
플랫폼 규칙: {platform_rule}
길이 규칙: {length_rule}

규칙:
1. 사진에서 실제로 확인할 수 있는 내용만 사용하고 장소나 인물을 임의로 추측하지 마세요.
2. 광고처럼 과장하지 말고 일반 사용자가 직접 쓴 자연스러운 말투로 작성하세요.
3. 게시글은 반드시 정확히 두 문장으로 작성하고 줄바꿈을 넣지 마세요.
4. 전체 게시글은 어떤 길이 설정에서도 절대 80자를 넘지 마세요.
5. 사진 내용과 분위기에 관련된 해시태그를 5개에서 8개 작성하세요.
6. 모든 해시태그는 #으로 시작하고 공백으로 구분하세요.
7. 설명이나 markdown 없이 다음 JSON 형식만 반환하세요.

{{"post":"게시글 두 문장","hashtags":"#태그1 #태그2 #태그3 #태그4 #태그5"}}
""".strip()

    payload = {
        "contents": [
            {
                "parts": [
                    {"text": prompt},
                    {"inline_data": {"mime_type": mime_type, "data": encoded_image}},
                ]
            }
        ],
        "generationConfig": {
            "maxOutputTokens": 384,
            "thinkingConfig": {"thinkingLevel": "MINIMAL"},
        },
    }
    response = requests.post(url, json=payload, timeout=(10, 90))
    if not response.ok:
        raise RuntimeError(_extract_gemini_error(response))

    candidates = response.json().get("candidates") or []
    if not candidates:
        raise RuntimeError("Gemini가 게시글 추천 결과를 반환하지 않았습니다.")
    parts = candidates[0].get("content", {}).get("parts", [])
    raw_text = "".join(
        str(part.get("text", "")) for part in parts if isinstance(part, dict)
    ).strip()
    if raw_text.startswith("```"):
        raw_text = raw_text.split("\n", 1)[-1]
        if raw_text.rstrip().endswith("```"):
            raw_text = raw_text.rstrip()[:-3].rstrip()

    try:
        result = json.loads(raw_text)
    except json.JSONDecodeError as exc:
        raise RuntimeError("Gemini 게시글 추천 결과 형식이 올바르지 않습니다.") from exc

    post = str(result.get("post", "")).strip().replace("\r", " ").replace("\n", " ")
    hashtags = str(result.get("hashtags", "")).strip().replace("\r", " ").replace("\n", " ")
    if not post or not hashtags:
        raise RuntimeError("AI 게시글 또는 해시태그 추천 결과가 비어 있습니다.")
    return {"post": post[:160], "hashtags": hashtags[:320]}


@app.get("/health")
def health():
    return {
        "status": "ok",
        "service": "imageeditor-integrated-ai",
        "mode": "local-cpu-with-replicate-restoration",
        "port": 8000,
        "features": [
            "object-mask-preview",
            "object-removal",
            "background-removal",
            "photo-restoration",
            "music-recommendation",
            "caption-recommendation",
            "social-post-recommendation",
            "community-face-filter-auto-fit",
        ],
        "replicate_configured": bool(_get_replicate_api_token()),
        "gemini_configured": bool(_get_gemini_api_key()),
    }


@app.get("/models/status")
def model_status():
    return {
        "loaded": {
            "mobile_sam": _sam_model is not None,
            "lama": _lama_model is not None,
            "u2netp": _background_session is not None,
            "colorization": _colorization_net is not None,
            "detail_enhancement": _detail_net is not None,
            "face_detector": _face_detector is not None,
        },
        "downloaded_files": {
            "mobile_sam.pt": (MODEL_DIR / "mobile_sam.pt").is_file(),
            **{
                model_name: (MODEL_DIR / model_name).is_file()
                for model_name in MODEL_FILES
            },
        },
        "model_cache_ascii": str(MODEL_DIR).isascii(),
        "replicate_model": REPLICATE_MODEL,
        "replicate_configured": bool(_get_replicate_api_token()),
        "gemini_configured": bool(_get_gemini_api_key()),
    }


@app.post("/detect-faces")
async def detect_faces(file: UploadFile = File(...)):
    data = await _read_uploaded_image(file)

    try:
        pil_image, _ = _decode_uploaded_image(data)
        rgb = np.asarray(pil_image, dtype=np.uint8)
        return await run_in_threadpool(_detect_faces, rgb)
    except ModelDownloadError as exc:
        raise HTTPException(status_code=503, detail=str(exc)) from exc
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except RuntimeError as exc:
        logger.exception("Community face detection is unavailable")
        raise HTTPException(status_code=503, detail=str(exc)) from exc
    except Exception as exc:
        logger.exception("Community face detection failed")
        raise HTTPException(
            status_code=500,
            detail=f"얼굴 자동 인식 처리 실패: {exc}",
        ) from exc


@app.post("/preview-mask")
async def preview_mask(
    file: UploadFile = File(...),
    mode: str = Form("POINT"),
    x: int = Form(0),
    y: int = Form(0),
    x1: int = Form(0),
    y1: int = Form(0),
    x2: int = Form(0),
    y2: int = Form(0),
):
    data = await _read_uploaded_image(file)

    try:
        _, bgr = _decode_uploaded_image(data)
        rgb = cv2.cvtColor(bgr, cv2.COLOR_BGR2RGB)
        mask = await run_in_threadpool(
            _get_sam_mask,
            rgb,
            mode,
            x,
            y,
            x1,
            y1,
            x2,
            y2,
        )
        result_bytes = await run_in_threadpool(_preview_object_mask, bgr, mask)
        return Response(content=result_bytes, media_type="image/png")
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except Exception as exc:
        logger.exception("Object mask preview failed")
        raise HTTPException(
            status_code=500,
            detail=f"AI 물체 인식 처리 실패: {exc}",
        ) from exc


@app.post("/remove-object")
async def remove_object(
    file: UploadFile = File(...),
    mode: str = Form("POINT"),
    x: int = Form(0),
    y: int = Form(0),
    x1: int = Form(0),
    y1: int = Form(0),
    x2: int = Form(0),
    y2: int = Form(0),
):
    data = await _read_uploaded_image(file)

    try:
        _, bgr = _decode_uploaded_image(data)
        rgb = cv2.cvtColor(bgr, cv2.COLOR_BGR2RGB)
        mask = await run_in_threadpool(
            _get_sam_mask,
            rgb,
            mode,
            x,
            y,
            x1,
            y1,
            x2,
            y2,
        )
        result_bytes = await run_in_threadpool(_remove_object_with_lama, rgb, mask)
        return Response(content=result_bytes, media_type="image/png")
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except Exception as exc:
        logger.exception("Object removal failed")
        raise HTTPException(
            status_code=500,
            detail=f"AI 물체 제거 처리 실패: {exc}",
        ) from exc


@app.post("/analyze-mood")
async def analyze_mood(file: UploadFile = File(...)):
    data = await _read_uploaded_image(file)
    api_key = _get_gemini_api_key()
    if not api_key:
        raise HTTPException(
            status_code=503,
            detail=(
                "음악 추천을 사용하려면 AIIntegratedServer\\2_set_gemini_key.bat를 "
                "먼저 실행해 주세요."
            ),
        )

    try:
        # 실제 이미지인지 먼저 검사한 뒤 Gemini에 전달합니다.
        await run_in_threadpool(_decode_uploaded_image, data)
        mime_type = file.content_type or "image/png"
        recommendation = await run_in_threadpool(
            _request_music_recommendation,
            data,
            mime_type,
            api_key,
        )
        logger.info("Music recommendation succeeded")
        return JSONResponse(content={"recommendation": recommendation})
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except requests.Timeout as exc:
        raise HTTPException(
            status_code=504,
            detail="Gemini 응답 시간이 초과되었습니다. 잠시 후 다시 시도해 주세요.",
        ) from exc
    except requests.RequestException as exc:
        logger.exception("Gemini connection failed")
        raise HTTPException(
            status_code=502,
            detail="Gemini 서버에 연결하지 못했습니다. 인터넷 연결을 확인해 주세요.",
        ) from exc
    except Exception as exc:
        logger.exception("Music recommendation failed")
        raise HTTPException(
            status_code=502,
            detail=f"Gemini 음악 추천 실패: {exc}",
        ) from exc


@app.post("/generate-captions")
async def generate_captions(file: UploadFile = File(...)):
    data = await _read_uploaded_image(file)
    api_key = _get_gemini_api_key()
    if not api_key:
        raise HTTPException(
            status_code=503,
            detail=(
                "AI 문구 추천을 사용하려면 AIIntegratedServer\\2_set_gemini_key.bat를 "
                "먼저 실행해 주세요."
            ),
        )

    try:
        await run_in_threadpool(_decode_uploaded_image, data)
        mime_type = file.content_type or "image/png"
        captions = await run_in_threadpool(
            _request_caption_recommendations,
            data,
            mime_type,
            api_key,
        )
        logger.info("Caption recommendation succeeded")
        return JSONResponse(content={"captions": captions})
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except requests.Timeout as exc:
        raise HTTPException(
            status_code=504,
            detail="Gemini 응답 시간이 초과되었습니다. 잠시 후 다시 시도해 주세요.",
        ) from exc
    except requests.RequestException as exc:
        logger.exception("Gemini caption connection failed")
        raise HTTPException(
            status_code=502,
            detail="Gemini 서버에 연결하지 못했습니다. 인터넷 연결을 확인해 주세요.",
        ) from exc
    except Exception as exc:
        logger.exception("Caption recommendation failed")
        raise HTTPException(
            status_code=502,
            detail=f"Gemini 문구 추천 실패: {exc}",
        ) from exc


@app.post("/social-post")
async def social_post(
    file: UploadFile = File(...),
    platform: str = Form("Instagram"),
    tone: str = Form("감성적"),
    length: str = Form("보통"),
):
    data = await _read_uploaded_image(file)
    api_key = _get_gemini_api_key()
    if not api_key:
        raise HTTPException(
            status_code=503,
            detail=(
                "AI 게시글 추천을 사용하려면 "
                "AIIntegratedServer\\2_set_gemini_key.bat를 먼저 실행해 주세요."
            ),
        )

    if platform not in {"Instagram", "Threads", "Facebook", "X"}:
        platform = "Instagram"
    if tone not in {"감성적", "일상", "유머", "깔끔한", "힙한", "친근한"}:
        tone = "감성적"
    if length not in {"짧게", "보통", "길게"}:
        length = "보통"

    try:
        await run_in_threadpool(_decode_uploaded_image, data)
        mime_type = file.content_type or "image/png"
        recommendation = await run_in_threadpool(
            _request_social_post_recommendation,
            data,
            mime_type,
            api_key,
            platform,
            tone,
            length,
        )
        logger.info("Social post recommendation succeeded")
        return JSONResponse(content=recommendation)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except requests.Timeout as exc:
        raise HTTPException(
            status_code=504,
            detail="Gemini 응답 시간이 초과되었습니다. 잠시 후 다시 시도해 주세요.",
        ) from exc
    except requests.RequestException as exc:
        logger.exception("Gemini social post connection failed")
        raise HTTPException(
            status_code=502,
            detail="Gemini 서버에 연결하지 못했습니다. 인터넷 연결을 확인해 주세요.",
        ) from exc
    except Exception as exc:
        logger.exception("Social post recommendation failed")
        raise HTTPException(
            status_code=502,
            detail=f"AI 게시글·해시태그 추천 실패: {exc}",
        ) from exc


@app.post("/remove-background")
async def remove_background(file: UploadFile = File(...)):
    if not file.content_type or not file.content_type.startswith("image/"):
        raise HTTPException(status_code=400, detail="이미지 파일만 업로드할 수 있습니다.")

    data = await file.read(MAX_UPLOAD_BYTES + 1)
    if not data:
        raise HTTPException(status_code=400, detail="빈 이미지 파일입니다.")
    if len(data) > MAX_UPLOAD_BYTES:
        raise HTTPException(status_code=413, detail="이미지 파일은 25MB 이하여야 합니다.")

    def process() -> bytes:
        image = Image.open(BytesIO(data))
        image = ImageOps.exif_transpose(image).convert("RGBA")
        result = _remove_background_with_u2netp(image)
        output = BytesIO()
        result.convert("RGBA").save(output, format="PNG", optimize=True)
        return output.getvalue()

    try:
        result_bytes = await run_in_threadpool(process)
        return Response(content=result_bytes, media_type="image/png")
    except Exception as exc:
        logger.exception("Background removal failed")
        raise HTTPException(
            status_code=500,
            detail=f"AI 배경 제거 처리 실패: {exc}",
        ) from exc


@app.post("/restore-photo")
async def restore_photo(
    file: UploadFile = File(...),
    restore_damage: bool = Form(True),
    colorize: bool = Form(True),
    enhance_details: bool = Form(True),
):
    data = await _read_uploaded_image(file)

    try:
        pil_image, bgr = _decode_uploaded_image(data)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc

    replicate_token = _get_replicate_api_token()
    fallback_reason = "token-missing"
    if replicate_token:
        try:
            result_bytes = await run_in_threadpool(
                _request_replicate_photo_restoration,
                pil_image,
                replicate_token,
            )
            logger.info("Photo restoration succeeded with Replicate")
            return Response(
                content=result_bytes,
                media_type="image/png",
                headers={
                    "X-ImageEditor-AI": "replicate-cloud",
                    "X-ImageEditor-Restoration-Mode": "replicate",
                    "X-ImageEditor-Colorized": "true",
                },
            )
        except Exception as exc:
            fallback_reason = "replicate-error"
            logger.exception("Replicate restoration failed; using local fallback: %s", exc)

    try:
        result, was_colorized = await run_in_threadpool(
            _restore_photo,
            bgr,
            restore_damage,
            colorize,
            enhance_details,
        )
        result_bytes = await run_in_threadpool(_encode_png, result)
        return Response(
            content=result_bytes,
            media_type="image/png",
            headers={
                "X-ImageEditor-AI": "local-cpu",
                "X-ImageEditor-Restoration-Mode": "local-fallback",
                "X-ImageEditor-Fallback-Reason": fallback_reason,
                "X-ImageEditor-Colorized": str(was_colorized).lower(),
            },
        )
    except ModelDownloadError as exc:
        logger.exception("Photo restoration model download failed")
        raise HTTPException(status_code=503, detail=str(exc)) from exc
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except Exception as exc:
        logger.exception("Photo restoration failed")
        raise HTTPException(
            status_code=500,
            detail=f"Replicate와 로컬 AI 사진 복원에 모두 실패했습니다: {exc}",
        ) from exc


if __name__ == "__main__":
    import uvicorn

    print("=" * 52)
    print("ImageEditor 통합 AI 서버")
    print("주소: http://127.0.0.1:8000")
    print("확인: http://127.0.0.1:8000/docs")
    print(
        "사진 복원: "
        + ("Replicate 연결됨" if _get_replicate_api_token() else "로컬 대체 모드 (Replicate 토큰 없음)")
    )
    print("종료: 이 창에서 Ctrl+C")
    print("=" * 52)
    uvicorn.run(app, host="127.0.0.1", port=8000)
