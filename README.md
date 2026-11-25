# AI 이미지 편집 프로그램 (ImageEditor)

계정 인증, 타일 분할 방식의 로컬 AI 이미지 업스케일링, 스냅샷 이력 저장, 편집 스타일(프리셋) 저장을 지원하는 C# WinForms 데스크톱 이미지 편집기 팀 프로젝트입니다.

- 기간: 2026.08.10 ~ 2026.08.21
- 인원: 6명 (팀장 1, 부팀장 1, 팀원 4) — 팀 프로젝트

## 구성

| 폴더 | 내용 |
|---|---|
| `ImageEditor/Login/` | 로그인, 회원가입, 아이디 찾기, 비밀번호 재설정 |
| `ImageEditor/Models/AI/` | ONNX Runtime 기반 AI 업스케일링 |
| `ImageEditor/Save/` | 편집 타임라인(스냅샷) 저장·복원 |
| `ImageEditor/Controls/` | 필터, 보정, 자르기, 크기 조정, 텍스트, SNS 비율, 템플릿, 내 편집 스타일 등 편집 도구 UI |
| `ImageEditor/Tools/` | AI 배경 제거, AI 물체 제거, 자르기, SNS 비율 도구 |
| `AIIntegratedServer/` | 배경 제거·물체 제거·사진 복원 등을 처리하는 Python FastAPI 로컬 AI 서버 |

**기술**: C#, WinForms, .NET 8, MySQL, BCrypt.Net, ONNX Runtime(DirectML), OpenCvSharp, Python(FastAPI)

## 담당 파트 (유동호)

> 통합 과정에서 팀원 코드와 합쳐져, 아래 경로는 해당 기능이 최종적으로 들어 있는 위치입니다.

- **회원 인증 및 비밀번호 해싱**: MySQL 기반 로그인과 BCrypt 비밀번호 해싱 저장
  - 관련 위치: `ImageEditor/Login/PasswordSecurity.cs`, `ImageEditor/Login/LoginForm.cs`, `ImageEditor/Login/JoinMembership.cs`
- **타일 분할 AI 업스케일링**: 128×128 타일 단위 Real-ESRGAN 추론 후 Lanczos 다운샘플링으로 원본 크기에 반영해 메모리 사용량 절감
  - 관련 위치: `ImageEditor/Models/AI/IAiUpscaler.cs`
- **비동기 스냅샷 저장 및 편집 프리셋**: SemaphoreSlim으로 동시 저장을 제어하는 비동기 타임라인 스냅샷, 델리게이트 기반 편집 스타일 저장·적용
  - 관련 위치: `ImageEditor/Save/TimeLinePage.cs`, `ImageEditor/Controls/MyEditStyleControl.cs`

## 실행

1. MySQL을 준비합니다. 접속 정보는 환경 변수로 지정할 수 있으며, 없으면 기본값을 사용합니다.
   - `IMAGEEDITOR_DB_SERVER` (기본 `localhost`), `IMAGEEDITOR_DB_PORT` (기본 `3306`), `IMAGEEDITOR_DB_NAME` (기본 `imageeditor`), `IMAGEEDITOR_DB_USER`, `IMAGEEDITOR_DB_PASSWORD`
2. AI 서버를 사용하려면 Python 3.11.9 64-bit 설치 후 `AIIntegratedServer/1_setup_server.bat` → `3_start_server.bat`를 실행합니다. 자세한 내용은 `0_Python311_먼저읽기.txt`를 참고하세요.
3. `ImageEditor.sln`을 Visual Studio 2022로 열어 실행합니다.

## 사용 모델 및 라이선스

- Real-ESRGAN (`real_esrgan_x4plus.onnx`, `RealESR-AnimeVideo-v3_x4.onnx`): https://github.com/xinntao/Real-ESRGAN
- AI 서버에서 사용하는 모델의 출처·라이선스는 `AIIntegratedServer/THIRD_PARTY_NOTICES.md`를 참고하세요.