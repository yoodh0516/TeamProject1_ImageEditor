# AI 배경 제거

- 기존 이미지 편집 기능은 그대로 유지합니다.
- AI 배경 제거를 포함한 모든 AI 요청은 통합 FastAPI 서버(`127.0.0.1:8000`)로 전송됩니다.
- 배경 제거는 U2NetP 모델을 사용자 PC의 CPU에서 직접 실행합니다.
- 최초 설치 및 모델 다운로드에는 인터넷 연결이 필요합니다.
- 결과는 RGBA PNG로 처리되어 투명 배경을 유지합니다.

## 처음 한 번

Python 3.11.9 64-bit를 설치한 뒤 `AIIntegratedServer/1_setup_server.bat`를
실행합니다. 패키지는 `AIIntegratedServer/.venv311`에 격리되어 설치됩니다.

이 서버는 Replicate를 호출하지 않으므로 API 토큰이나 크레딧이 필요하지 않습니다.

## 사용

1. `AIIntegratedServer/3_start_server.bat`를 실행하고 창을 열어 둡니다.
2. Visual Studio에서 ImageEditor를 실행합니다.
3. 다음 메뉴를 사용합니다.

`AI` → `AI 배경 제거` → `AI로 배경 제거`

처리 결과를 확인한 뒤 `적용하기`를 누르면 현재 이미지에 적용됩니다.
투명 배경을 유지하려면 PNG 형식으로 저장하세요.
