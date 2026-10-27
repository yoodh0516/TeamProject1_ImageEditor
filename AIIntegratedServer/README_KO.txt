ImageEditor 통합 FastAPI AI 서버 (포트 8000)

이 폴더의 server.py 하나가 ImageEditor의 모든 AI 요청을 받습니다.

[제공 기능]
- POST /preview-mask      : MobileSAM으로 물체 선택 영역 미리보기 (로컬 CPU)
- POST /remove-object     : MobileSAM + LaMa 물체 제거 (로컬 CPU)
- POST /remove-background : U2NetP 배경 제거 (로컬 CPU)
- POST /restore-photo     : Replicate 고품질 복원 (실패 시 로컬 CPU 자동 대체)
- POST /analyze-mood      : 이미지 분위기 분석 후 음악 추천 (Gemini API)
- POST /generate-captions : 기존 자동 문구 추천 (Gemini API)
- POST /social-post       : 게시글 2문장과 해시태그 추천 (Gemini API)
- POST /detect-faces      : 커뮤니티 필터용 얼굴·코·기울기 자동 인식 (MediaPipe, 로컬 CPU)
- GET  /health            : 서버 연결 상태 확인
- GET  /models/status     : 모델 준비 상태 확인

[처음 한 번]
1. Python 3.11.9 64-bit를 사용합니다.
   cmd에서 py -3.11 --version을 입력해 버전을 확인할 수 있습니다.
   Python을 다시 설치할 경우 Add python.exe to PATH를 체크합니다.
2. 1_setup_server.bat를 더블클릭합니다.
   이 파일은 Python 3.11.9 전용 .venv311 환경과 호환 패키지를 자동 설치합니다.
3. 고품질 사진 복원을 위해 2_set_replicate_token.bat를 실행합니다.
   토큰을 저장한 뒤에는 서버를 다시 시작하지 않아도 됩니다.
4. 음악·자동 문구·게시글 추천을 사용할 경우 2_set_gemini_key.bat를 실행합니다.
   키를 저장한 뒤에는 서버를 다시 시작하지 않아도 됩니다.

[매번 프로그램을 사용할 때]
1. 3_start_server.bat를 더블클릭합니다.
2. 검은 서버 창을 닫지 않은 상태로 둡니다.
3. 필요하면 4_check_server.bat로 연결을 확인합니다.
4. Visual Studio에서 ImageEditor.sln을 실행합니다.

[직접 명령어로 실행하려면]
cd AIIntegratedServer
.venv311\Scripts\activate
python server.py

[주소]
- 서버: http://127.0.0.1:8000
- API 테스트 화면: http://127.0.0.1:8000/docs

[중요]
- 그래픽카드가 없어도 됩니다. Replicate 사진 복원은 외부 클라우드 GPU를 사용하고
  물체 제거·배경 제거 등 로컬 모델은 CPU로 강제 실행합니다.
- 첫 기능 실행 때 모델 파일을 내려받으므로 인터넷 연결이 필요합니다.
- 모델은 Windows 한글 경로 오류를 피하도록
  %LOCALAPPDATA%\ImageEditorAI\models 폴더에 저장됩니다.
- 이전 AIIntegratedServer\models에 받은 모델은 자동으로 새 폴더에 복사됩니다.
- 모델 다운로드와 CPU 추론은 PC 성능에 따라 몇 분 걸릴 수 있습니다.
- 물체 제거와 배경 제거에는 별도 API 키나 호출 비용이 없습니다.
- 커뮤니티 얼굴 자동 맞춤은 MediaPipe를 로컬 CPU에서 실행하며 별도 API 키가 없습니다.
- 사진 복원은 Replicate API 토큰, 인터넷, 계정 크레딧이 필요하며 실행마다 비용이
  발생합니다. Replicate 오류가 나면 기존 로컬 CPU 복원으로 자동 전환합니다.
- 음악·자동 문구·게시글 추천은 Gemini API 키와 인터넷 연결이 필요하며 Google 정책에 따라 비용이 발생할 수 있습니다.
- API 토큰과 키는 server.py나 ZIP에 넣지 않고 Windows 사용자 전용 설정 폴더에 보관합니다.
- 서버를 끄려면 서버 창에서 Ctrl+C를 누릅니다.

[문제 해결]
- "서버가 실행되지 않았습니다": 3_start_server.bat 창이 켜져 있는지 확인합니다.
- "Python 3.11.9 64-bit를 찾지 못했습니다": cmd에서 py -3.11 --version을 확인하고,
  Python 설치 후 Windows 탐색기와 cmd를 완전히 닫았다가 다시 엽니다.
- 예전 설치가 일부 남아 오류가 나면 .venv311 폴더만 삭제한 뒤
  1_setup_server.bat를 다시 실행합니다.
- 포트 8000 사용 중: 기존 Python 서버 창을 모두 닫고 다시 실행합니다.
- Gemini 추천 기능 키 오류: 새 2_set_gemini_key.bat를 한 번 실행합니다.
- 복원 결과에 "로컬 CPU 대체 복원 (Replicate 토큰 없음)"이 표시됨:
  새 2_set_replicate_token.bat를 실행하고 r8_로 시작하는 토큰을 입력합니다.
- "Replicate 연결 실패" 대체 복원: 인터넷, 토큰, Replicate 크레딧을 확인합니다.
- OpenCV "Can't open" 또는 "Illegal byte sequence": 이 수정본으로 덮어쓴 뒤
  서버를 다시 실행하면 모델이 영문 캐시로 자동 이동됩니다.
- 세피아 사진이 그대로 나오는 문제: 3.2 수정본은 세피아 단색 사진도 자동으로
  판별해 컬러 복원하며, 얼굴과 하단 글자를 보호하면서 선형 균열을 복원합니다.
- 이미지 열기 뒤 사진이나 프로그램 창이 너무 커짐: 4.0 수정본은 작은 이미지를
  100%보다 확대하지 않고 이미지 열기 전 창 크기와 상태를 유지합니다.
- 자세한 Python 오류: server.log의 마지막 부분을 확인합니다.
