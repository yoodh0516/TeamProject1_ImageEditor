# Integrated AI server notices

The local server downloads pretrained model weights on first use. Model files
are not included in this source archive.

- **Colorful Image Colorization** by Richard Zhang, Phillip Isola and Alexei A.
  Efros. Source and model architecture: <https://github.com/richzhang/colorization>
  (BSD-2-Clause). The Caffe weight file is verified with SHA-256 before use.
- **FSRCNN TensorFlow / OpenCV model** by Saafke. Source and weights:
  <https://github.com/Saafke/FSRCNN_Tensorflow> (Apache-2.0).
- **U2NetP background-removal model**. The ONNX weight is downloaded from the
  rembg project release and executed directly with ONNX Runtime; the rembg
  Python package is not installed. Project/model information:
  <https://github.com/danielgatis/rembg>.
- **Ultralytics MobileSAM**. Software and model information:
  <https://github.com/ultralytics/ultralytics>. Ultralytics is distributed
  under AGPL-3.0, with a separate enterprise license available from its
  publisher. Confirm that the chosen license fits any distribution or
  commercial use of this application.
- **LaMa inpainting**. The server contains a small Python 3.11-compatible
  TorchScript adapter based on the workflow published by
  <https://github.com/enesmsahin/simple-lama-inpainting> (Apache-2.0), but does
  not install that legacy wrapper package. The underlying LaMa research/model
  terms should also be reviewed for the target use.
- **Google MediaPipe Face Detector / BlazeFace full-range model** is used for
  local community-filter face placement. MediaPipe and the BlazeFace model are
  distributed under the Apache License 2.0; no face image is sent to Google.
- **Gemini API** is used by the optional music, caption, and social-post
  recommendation endpoints.
  Its service terms, quota, and pricing are controlled by Google.
- **Replicate API / flux-kontext-apps/restore-image** is used for cloud photo
  restoration. Input photos are sent to Replicate, each call can consume account
  credit, and Replicate's service terms, retention, and pricing apply.

Downloaded model weights are stored in the per-user `ImageEditorAI/models`
application-data directory so Windows native libraries never receive the
Korean project path.
