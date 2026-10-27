using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace ImageEditor.Controls
{
    /// <summary>
    /// 메뉴와 동적으로 생성되는 편집 컨트롤에 일관된 도움말을 제공한다.
    /// 기존 ToolTip이 등록된 컨트롤은 그 문구를 우선 사용한다.
    /// </summary>
    internal static class EditorToolTipService
    {
        private static readonly Dictionary<string, string> NameHelp =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["txtWidth"] = "변경할 이미지의 가로 크기를 픽셀 단위로 입력합니다.",
                ["txtHeight"] = "변경할 이미지의 세로 크기를 픽셀 단위로 입력합니다.",
                ["chkKeepRatio"] = "가로나 세로 중 하나를 바꾸면 원본 비율에 맞춰 반대쪽 값을 계산합니다.",
                ["cmbPreset"] = "미리 확인할 SNS 이미지 규격을 선택합니다.",
                ["picCanvas"] = "현재 이미지와 레이어 편집 결과가 표시되는 작업 캔버스입니다.",
                ["tbID"] = "ImageEditor 계정 아이디를 입력합니다.",
                ["tbPASS"] = "ImageEditor 계정 비밀번호를 입력합니다.",
                ["tbPASSCheck"] = "입력한 비밀번호를 한 번 더 입력합니다.",
                ["tbNAME"] = "회원 이름을 입력합니다.",
                ["tbEMAIL"] = "계정 확인에 사용할 이메일 주소를 입력합니다.",
                ["tbResetPASS"] = "새로 사용할 비밀번호를 입력합니다.",
                ["tbResetPASSCheck"] = "새 비밀번호를 한 번 더 입력합니다.",
                ["btnApply"] = "미리보기 결과를 실제 편집 상태에 적용합니다.",
                ["btnReset"] = "현재 기능의 설정만 기본값으로 되돌립니다.",
                ["btnCancel"] = "미적용 변경을 버리고 기능을 열기 전 상태로 돌아갑니다.",
                ["btnConfirm"] = "입력한 내용을 확인하고 작업을 완료합니다.",
                ["btnBack"] = "현재 창을 닫고 이전 화면으로 돌아갑니다.",
                ["btnClose"] = "현재 미리보기 창을 닫습니다.",
                ["btnFindID"] = "입력한 이름과 이메일로 가입 아이디를 찾습니다.",
                ["btnFindPass"] = "입력한 계정 정보가 일치하는지 확인합니다.",
                ["btnPanelFIndID"] = "아이디 찾기 입력 화면을 표시합니다.",
                ["btnPanelFIndPass"] = "비밀번호 찾기 입력 화면을 표시합니다.",
                ["trackBrightness"] = "이미지의 밝기를 조절합니다. 이동 중에는 미리보기만 변경됩니다.",
                ["trackContrast"] = "밝고 어두운 영역의 대비를 조절합니다. 이동 중에는 미리보기만 변경됩니다.",
                ["trackSaturation"] = "색상의 선명한 정도를 조절합니다. 이동 중에는 미리보기만 변경됩니다.",
                ["trackTemperature"] = "이미지 색감을 차갑거나 따뜻하게 조절합니다.",
                ["btnVivid"] = "색을 화사하고 선명하게 보정하는 필터를 미리봅니다.",
                ["btnBright"] = "전체적으로 밝고 깨끗한 분위기의 필터를 미리봅니다.",
                ["btnCozy"] = "따뜻하고 아늑한 분위기의 필터를 미리봅니다.",
                ["btnElegant"] = "차분하고 우아한 분위기의 필터를 미리봅니다.",
                ["btnBlackWhite"] = "이미지를 흑백 분위기로 변환해 미리봅니다.",
                ["btnCool"] = "차갑고 시원한 색감의 필터를 미리봅니다.",
                ["btnPolaroid"] = "폴라로이드 프레임 템플릿을 미리봅니다.",
                ["btnFilm"] = "필름 프레임 템플릿을 미리봅니다.",
                ["btnMagazine"] = "매거진 표지 스타일 템플릿을 미리봅니다.",
                ["btnPhotoCard"] = "포토카드 스타일 템플릿을 미리봅니다.",
                ["btnInstagram"] = "인스타그램 게시물 스타일 템플릿을 미리봅니다.",
                ["btnBirthday"] = "생일카드 스타일 템플릿을 미리봅니다.",
                ["btnPostcard"] = "여행엽서 스타일 템플릿을 미리봅니다.",
                ["btnMinimalPoster"] = "미니멀 포스터 스타일 템플릿을 미리봅니다.",
                ["btnTemplateReset"] = "현재 템플릿 선택과 설정을 기본값으로 되돌립니다.",
                ["lbMyEditStyle"] = "저장된 나만의 편집 스타일을 선택합니다.",
                ["tbSaveMyEditStyleName"] = "저장할 편집 스타일의 이름을 입력합니다.",
                ["btnLoad"] = "목록에서 선택한 편집 스타일을 현재 이미지에 적용합니다.",
                ["btnSaveMyEditStyle"] = "현재 편집 설정을 입력한 이름으로 저장합니다.",
                ["trkSize"] = "선택한 스티커의 크기를 조절합니다.",
                ["trkRotation"] = "선택한 스티커의 회전 각도를 조절합니다.",
                ["chkCommunityAutoFit"] = "얼굴을 인식해 선택한 장식의 위치와 크기를 자동으로 맞춥니다.",
                ["picBoldGlam"] = "토끼 얼굴 스티커를 선택합니다.",
                ["picCuteHeart"] = "하트 스티커를 선택합니다.",
                ["picDogLens"] = "강아지 얼굴 스티커를 선택합니다.",
                ["picFlowerCrown"] = "꽃 왕관 스티커를 선택합니다.",
                ["picSoftGlam"] = "공주 왕관 스티커를 선택합니다.",
                ["picRetroStory"] = "천사 후광 스티커를 선택합니다.",
                ["picKBeauty"] = "악마 뿔 스티커를 선택합니다.",
                ["picCatFace"] = "고양이 얼굴 스티커를 선택합니다.",
                ["cmbSocialPlatform"] = "게시글을 작성할 SNS 플랫폼을 선택합니다.",
                ["cmbPostTone"] = "추천 게시글의 말투와 분위기를 선택합니다.",
                ["cmbPostLength"] = "추천 게시글의 분량을 선택합니다.",
                ["txtPostResult"] = "AI가 추천한 SNS 게시글 결과를 확인합니다.",
                ["txtHashtagResult"] = "AI가 추천한 해시태그 결과를 확인합니다.",
                ["cmbTextFont"] = "텍스트 레이어에 사용할 글꼴을 선택합니다.",
                ["numTextFontSize"] = "텍스트 글꼴 크기를 픽셀 단위로 조절합니다.",
                ["trkTextOpacity"] = "텍스트의 불투명도를 조절합니다.",
                ["cmbTextEffect"] = "텍스트에 적용할 그림자·윤곽선 등의 효과를 선택합니다.",
                ["cmbMaskShape"] = "마스크 영역을 그릴 브러시 또는 도형 방식을 선택합니다.",
                ["trkMaskBrushSize"] = "마스크 브러시의 크기를 조절합니다.",
                ["trkDrawingBrushSize"] = "그리기 브러시의 굵기를 조절합니다.",
                ["trkDrawingOpacity"] = "그리기 브러시의 불투명도를 조절합니다.",
                ["btnDrawingQuickColor"] = "이 색상을 그리기 브러시 색상으로 선택합니다.",
                ["btnDrawingQuickSize"] = "표시된 픽셀 크기를 그리기 브러시 굵기로 선택합니다."
            };

        private static readonly Dictionary<string, string> TextHelp =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["파일"] = "이미지 열기, 저장, SNS용 내보내기 기능을 표시합니다.",
                ["편집"] = "실행 취소, 다시 실행, 원본 복원 기능을 표시합니다.",
                ["이미지"] = "크기 조절, 자르기, 회전과 반전 기능을 표시합니다.",
                ["보정·필터"] = "색상 보정과 필터 관련 기능을 표시합니다.",
                ["꾸미기"] = "템플릿, 텍스트, 그리기와 마스크 기능을 표시합니다.",
                ["AI"] = "AI 이미지 편집과 추천 기능을 표시합니다.",
                ["보기"] = "확대·축소, 화면 맞춤과 결과 비교 기능을 표시합니다.",
                ["이미지 열기"] = "편집할 이미지 파일을 불러옵니다.",
                ["열기"] = "편집할 이미지 파일을 불러옵니다.",
                ["저장"] = "현재 편집 결과를 기존 파일 또는 지정한 형식으로 저장합니다.",
                ["다른 이름으로 저장"] = "저장 위치와 파일 이름 및 형식을 새로 지정합니다.",
                ["SNS용 내보내기"] = "SNS 또는 웹 규격을 선택해 최적화된 JPG 파일로 저장합니다.",
                ["Instagram 정사각형"] = "현재 이미지를 1080 × 1080 규격으로 변환해 저장합니다.",
                ["Instagram 세로형"] = "현재 이미지를 1080 × 1350 규격으로 변환해 저장합니다.",
                ["세로 스토리/TikTok"] = "현재 이미지를 1080 × 1920 세로형 규격으로 변환해 저장합니다.",
                ["YouTube 썸네일"] = "현재 이미지를 1280 × 720 썸네일 규격으로 변환해 저장합니다.",
                ["웹/블로그 대표 이미지"] = "현재 이미지를 1200 × 630 웹 대표 이미지 규격으로 변환해 저장합니다.",
                ["종료"] = "ImageEditor를 종료합니다. 저장하지 않은 변경이 있으면 확인합니다.",
                ["실행 취소"] = "바로 이전 편집 작업으로 되돌립니다.",
                ["취소"] = "미적용 변경을 버리거나 바로 이전 편집 작업을 취소합니다.",
                ["다시 실행"] = "취소했던 편집 작업을 다시 적용합니다.",
                ["다시"] = "취소했던 편집 작업을 다시 적용합니다.",
                ["원본으로 되돌리기"] = "현재 편집 내용을 초기 이미지 상태로 되돌립니다.",
                ["크기 조절"] = "이미지의 가로와 세로 픽셀 크기를 변경합니다.",
                ["자르기"] = "캔버스에서 선택한 영역만 남기고 이미지를 자릅니다.",
                ["SNS 비율"] = "SNS 게시물 규격에 맞는 화면 비율을 선택합니다.",
                ["왼쪽 90° 회전"] = "이미지를 왼쪽으로 90도 회전하고 한 번 기록합니다.",
                ["오른쪽 90° 회전"] = "이미지를 오른쪽으로 90도 회전하고 한 번 기록합니다.",
                ["좌우 반전"] = "이미지를 좌우 방향으로 뒤집고 한 번 기록합니다.",
                ["상하 반전"] = "이미지를 위아래 방향으로 뒤집고 한 번 기록합니다.",
                ["사진 보정"] = "밝기, 대비, 채도와 색온도를 미리보며 조절합니다.",
                ["기본 필터"] = "현재 이미지 또는 선택 레이어에 기본 필터를 미리봅니다.",
                ["스티커"] = "스티커를 직접 배치하거나 얼굴 자동 인식으로 적용합니다.",
                ["나만의 편집 스타일"] = "자주 사용하는 편집 설정을 저장하고 다시 적용합니다.",
                ["내 편집 스타일"] = "저장된 개인 편집 스타일을 확인합니다.",
                ["템플릿"] = "사진과 텍스트가 포함된 디자인 템플릿을 적용합니다.",
                ["텍스트 추가"] = "캔버스에 편집 가능한 텍스트 레이어를 추가합니다.",
                ["그리기"] = "브러시 또는 지우개로 선택 레이어에 직접 그립니다.",
                ["마스크"] = "브러시로 선택 영역을 표시하거나 지워 마스크를 편집합니다.",
                ["AI 물체 제거"] = "표시한 영역을 AI 서버로 처리해 물체를 제거합니다.",
                ["포인터 모드"] = "한 지점을 선택해 주변 물체를 AI 제거 대상으로 지정합니다.",
                ["드래그 모드"] = "사각형 영역을 드래그해 AI 제거 대상으로 지정합니다.",
                ["AI 배경 제거"] = "주요 대상을 인식해 배경을 투명하게 제거합니다.",
                ["AI 오래된 사진 복원"] = "손상되거나 오래된 사진을 AI로 복원하고 결과를 미리봅니다.",
                ["AI 화질 개선"] = "선택한 AI 모델로 세부 묘사와 선명도를 개선합니다.",
                ["AI 인물 사진 자동보정"] = "인물 사진을 선택한 보정 스타일로 처리하고 결과를 미리봅니다.",
                ["음악 추천"] = "현재 이미지의 분위기를 분석해 어울리는 음악을 추천합니다.",
                ["유행어·밈 추천"] = "현재 이미지에 어울리는 유행어와 밈을 추천하고 텍스트 편집으로 연결합니다.",
                ["글씨 위치 추천"] = "이미지 구도를 분석해 선택한 문구의 배치 위치를 추천합니다.",
                ["게시글·해시태그 추천"] = "현재 사진에 어울리는 SNS 게시글과 해시태그를 추천합니다.",
                ["확대"] = "캔버스의 화면 배율을 높입니다. 원본 픽셀은 변경하지 않습니다.",
                ["축소"] = "캔버스의 화면 배율을 낮춥니다. 원본 픽셀은 변경하지 않습니다.",
                ["화면 맞춤"] = "이미지 전체가 캔버스 안에 보이도록 화면 배율을 맞춥니다.",
                ["B/A 비교"] = "편집 전 원본과 현재 편집 결과를 나란히 비교합니다.",
                ["Before/After 비교"] = "편집 전 원본과 현재 편집 결과를 나란히 비교합니다.",
                ["결과 미리보기"] = "SNS 규격별 최종 표시 결과를 별도 창에서 확인합니다.",
                ["타임라인 열기"] = "현재 편집 작업의 썸네일 타임라인을 펼칩니다.",
                ["타임라인 닫기"] = "현재 편집 작업의 썸네일 타임라인을 접습니다.",
                ["레이어 숨김"] = "왼쪽 레이어 패널을 접어 캔버스 공간을 넓힙니다.",
                ["레이어 열기"] = "접어 둔 레이어 패널을 다시 표시합니다.",
                ["상세 숨김"] = "오른쪽 상세 설정 패널을 접어 캔버스 공간을 넓힙니다.",
                ["상세 열기"] = "접어 둔 상세 설정 패널을 다시 표시합니다.",
                ["+ 빈 레이어"] = "그리기나 텍스트 작업에 사용할 투명 레이어를 추가합니다.",
                ["+ 이미지"] = "파일에서 이미지를 선택해 새 레이어로 추가합니다.",
                ["삭제"] = "현재 선택한 항목을 삭제합니다.",
                ["복제"] = "현재 선택한 레이어를 복제합니다.",
                ["▲ 위로"] = "선택 레이어를 한 단계 위로 이동합니다.",
                ["▼ 아래로"] = "선택 레이어를 한 단계 아래로 이동합니다.",
                ["표시"] = "선택 레이어의 표시 여부를 전환합니다.",
                ["잠금"] = "선택 레이어를 잠가 실수로 편집되지 않게 합니다.",
                ["잠금 해제"] = "선택 레이어의 잠금을 해제합니다.",
                ["✎  레이어 이름 변경"] = "선택 레이어의 이름을 변경합니다.",
                ["이미지 선택"] = "편집할 이미지 파일을 불러옵니다.",
                ["작업 타임라인"] = "현재 프로젝트의 편집 단계 썸네일을 표시합니다.",
                ["프로젝트"] = "로그인 계정의 편집 프로젝트 목록을 표시합니다.",
                ["프로젝트 삭제"] = "선택한 프로젝트와 저장된 타임라인 기록을 삭제합니다.",
                ["프로젝트 타임라인"] = "현재 계정의 프로젝트와 편집 기록을 엽니다.",
                ["로그아웃"] = "현재 계정에서 로그아웃하고 세션 타임라인을 정리합니다.",
                ["적용"] = "미리보기 결과를 실제 이미지 또는 선택 레이어에 확정합니다.",
                ["초기화"] = "현재 기능의 설정만 기본값으로 되돌립니다.",
                ["다시 선택"] = "현재 선택 영역을 지우고 새 영역을 선택합니다.",
                ["작업 취소"] = "현재 진행 중인 AI 요청을 취소합니다.",
                ["음악 추천 시작"] = "현재 이미지의 분위기를 분석해 음악 추천을 시작합니다.",
                ["추천 받기"] = "선택한 유형과 분위기로 현재 이미지에 어울리는 유행어와 밈을 추천합니다.",
                ["추천 위치로 편집 시작"] = "얼굴과 복잡한 배경을 피해 추천한 위치에서 텍스트 편집을 시작합니다.",
                ["AI 게시글 추천 시작"] = "선택한 플랫폼·분위기·길이로 게시글과 해시태그 추천을 시작합니다.",
                ["Real-ESRGAN 사진·풍경의 질감과 가장자리 보정"] = "사진과 풍경의 질감 및 가장자리를 세밀하게 개선하는 모델을 선택합니다.",
                ["AnimeVideo 일러스트·애니 이미지에 빠른 보정"] = "일러스트와 애니메이션 이미지에 적합한 빠른 모델을 선택합니다.",
                ["자연 보정 피부·색감·노출을 균형 있게 미세 보정"] = "피부, 색감과 노출을 균형 있게 조절하는 자연 보정을 선택합니다.",
                ["피부결 중심 잡티와 피부톤을 자연스럽게 정돈"] = "잡티와 피부톤 정돈을 중심으로 하는 인물 보정을 선택합니다.",
                ["색감·노출 중심 밝기·화이트밸런스·색감을 부드럽게 보정"] = "밝기, 화이트밸런스와 색감을 중심으로 하는 인물 보정을 선택합니다.",
                ["50%"] = "현재 기준 크기의 50% 값을 빠르게 입력하고 미리봅니다.",
                ["100%"] = "기능을 열었을 때의 기준 크기 100%로 되돌려 미리봅니다.",
                ["200%"] = "현재 기준 크기의 200% 값을 빠르게 입력하고 미리봅니다.",
                ["B"] = "선택한 텍스트의 굵게 스타일을 전환합니다.",
                ["I"] = "선택한 텍스트의 기울임 스타일을 전환합니다.",
                ["U"] = "선택한 텍스트의 밑줄 스타일을 전환합니다.",
                ["✎ 펜"] = "그리기 도구를 펜 모드로 전환합니다.",
                ["⌫ 지우개"] = "그리기 도구를 지우개 모드로 전환합니다.",
                ["색상 선택"] = "그리기 브러시에 사용할 색상을 선택합니다.",
                ["현재 레이어 전체 지우기"] = "현재 그리기 레이어의 모든 선을 지웁니다.",
                ["⟲ 이전으로 되돌리기"] = "마스크 작업 직전 상태로 되돌립니다.",
                ["선택 추가"] = "캔버스에서 칠한 영역을 현재 마스크 선택에 추가합니다.",
                ["선택 지우기"] = "캔버스에서 칠한 영역을 현재 마스크 선택에서 제외합니다.",
                ["선택 영역 지우기 (투명)"] = "마스크로 선택한 이미지 영역을 투명하게 지웁니다.",
                ["선택 영역 흑백"] = "마스크로 선택한 영역에 흑백 효과를 적용합니다.",
                ["선택 영역 흐림"] = "마스크로 선택한 영역에 흐림 효과를 적용합니다.",
                ["선택 영역 색상 반전"] = "마스크로 선택한 영역의 색상을 반전합니다.",
                ["효과 영역 뒤집기"] = "현재 효과의 적용 영역과 비적용 영역을 서로 바꿉니다.",
                ["선택 반전"] = "마스크의 선택 영역과 선택되지 않은 영역을 서로 바꿉니다.",
                ["선택 초기화"] = "현재 마스크 선택 영역을 모두 지웁니다.",
                ["게시글 복사"] = "추천된 게시글 문구를 클립보드에 복사합니다.",
                ["해시태그 복사"] = "추천된 해시태그를 클립보드에 복사합니다.",
                ["비회원으로 시작"] = "로그인하지 않고 편집을 시작합니다. 개인 기록은 저장되지 않습니다.",
                ["로그인"] = "입력한 계정 정보로 로그인합니다.",
                ["회원가입"] = "새 ImageEditor 계정을 만드는 화면을 엽니다.",
                ["회원가입 완료"] = "입력한 정보로 새 계정을 생성합니다.",
                ["아이디 찾기"] = "가입할 때 입력한 이름과 이메일로 아이디를 찾습니다.",
                ["비밀번호 찾기"] = "계정 정보를 확인한 뒤 사용할 비밀번호를 입력합니다.",
                ["본인 확인"] = "아이디, 이름과 이메일이 가입 정보와 일치하는지 확인합니다.",
                ["확인"] = "입력한 내용을 확인하고 작업을 완료합니다.",
                ["← 뒤로가기"] = "현재 창을 닫고 이전 화면으로 돌아갑니다.",
                ["닫기"] = "현재 창을 닫습니다."
            };

        internal static ToolTip Attach(Control owner)
        {
            ToolTip toolTip = new ToolTip();
            Configure(toolTip);
            _ = new Registration(owner, toolTip);
            return toolTip;
        }

        internal static void Configure(ToolTip toolTip)
        {
            toolTip.AutoPopDelay = 7000;
            toolTip.InitialDelay = 400;
            toolTip.ReshowDelay = 100;
            toolTip.ShowAlways = true;
            toolTip.UseAnimation = true;
            toolTip.UseFading = true;
            toolTip.IsBalloon = false;
        }

        internal static void Set(ToolTip toolTip, Control control, string helpText)
        {
            if (string.IsNullOrWhiteSpace(helpText))
                return;

            control.AccessibleDescription = helpText;
            if (string.IsNullOrWhiteSpace(control.AccessibleName))
            {
                string text = NormalizeText(control.Text);
                if (!string.IsNullOrWhiteSpace(text))
                    control.AccessibleName = text;
            }
            toolTip.SetToolTip(control, helpText);
        }

        internal static void ApplyMenuToolTips(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                if (item is ToolStripSeparator)
                    continue;

                ToolStripDropDownItem? dropDownItem =
                    item as ToolStripDropDownItem;
                string text = NormalizeText(item.Text);
                string help = ResolveTextHelp(
                    text,
                    dropDownItem != null && dropDownItem.DropDownItems.Count > 0);
                if (string.IsNullOrWhiteSpace(item.ToolTipText))
                    item.ToolTipText = help;
                item.AccessibleDescription = item.ToolTipText;
                if (string.IsNullOrWhiteSpace(item.AccessibleName))
                    item.AccessibleName = text;

                if (dropDownItem != null && dropDownItem.DropDownItems.Count > 0)
                    ApplyMenuToolTips(dropDownItem.DropDownItems);
            }
        }

        private static bool IsInteractive(Control control)
        {
            return control is ButtonBase ||
                control is TextBoxBase ||
                control is ComboBox ||
                control is TrackBar ||
                control is NumericUpDown ||
                control is ListBox ||
                control is CheckedListBox ||
                control is TabControl ||
                control is LinkLabel ||
                ((control is Label || control is PictureBox || control is Panel) &&
                 control.Cursor == Cursors.Hand);
        }

        private static string ResolveControlHelp(Control control)
        {
            if (!string.IsNullOrWhiteSpace(control.AccessibleDescription))
                return control.AccessibleDescription;

            if (!string.IsNullOrWhiteSpace(control.Name) &&
                NameHelp.TryGetValue(control.Name, out string? nameHelp))
            {
                return nameHelp;
            }

            string text = NormalizeText(control.Text);
            if (TextHelp.TryGetValue(text, out string? textHelp))
                return textHelp;

            if (text.StartsWith("AI ", StringComparison.OrdinalIgnoreCase))
                return $"{FirstLine(text)} 기능을 실행합니다. 처리 결과는 적용 전에 확인할 수 있습니다.";
            if (text.StartsWith("작업 ", StringComparison.OrdinalIgnoreCase))
                return $"{FirstLine(text)} 상태를 선택하거나 복원합니다.";

            if (control is TrackBar)
                return "값을 드래그하여 조절합니다. 조절 중에는 미리보기로 확인할 수 있습니다.";
            if (control is ComboBox)
                return "목록에서 사용할 옵션을 선택합니다.";
            if (control is NumericUpDown)
                return "화살표 또는 숫자 입력으로 값을 조절합니다.";
            if (control is TextBoxBase)
                return "이 기능에 사용할 값을 입력합니다.";
            if (control is RadioButton)
                return string.IsNullOrWhiteSpace(text)
                    ? "이 옵션 하나를 선택합니다."
                    : $"‘{FirstLine(text)}’ 옵션을 선택합니다.";
            if (control is CheckBox)
                return string.IsNullOrWhiteSpace(text)
                    ? "이 옵션의 사용 여부를 전환합니다."
                    : $"‘{FirstLine(text)}’ 옵션의 사용 여부를 전환합니다.";
            if (control is PictureBox)
                return "이미지 항목을 선택하거나 결과를 확인합니다.";
            if (control is TabControl)
                return "탭을 선택해 표시할 작업 영역을 전환합니다.";
            if (control is ListBox || control is CheckedListBox)
                return "목록에서 사용할 항목을 선택합니다.";
            if (control is ButtonBase)
            {
                if (string.IsNullOrWhiteSpace(text) &&
                    control.Name.StartsWith("btnDrawingQuickColor", StringComparison.OrdinalIgnoreCase))
                {
                    return "이 색상을 그리기 브러시 색상으로 선택합니다.";
                }
                return string.IsNullOrWhiteSpace(text)
                    ? "이 기능을 실행합니다."
                    : $"‘{FirstLine(text)}’ 기능을 실행합니다.";
            }

            return string.IsNullOrWhiteSpace(text)
                ? "이 항목을 선택합니다."
                : $"‘{FirstLine(text)}’ 항목을 선택합니다.";
        }

        private static string ResolveTextHelp(string text, bool hasChildren)
        {
            if (TextHelp.TryGetValue(text, out string? help))
                return help;
            if (hasChildren)
                return $"‘{FirstLine(text)}’ 관련 기능을 표시합니다.";
            return $"‘{FirstLine(text)}’ 기능을 실행합니다.";
        }

        private static string NormalizeText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return string.Join(" ", text
                .Replace("&", string.Empty)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => part.Length > 0));
        }

        private static string FirstLine(string text)
        {
            int separator = text.IndexOf("  ", StringComparison.Ordinal);
            return separator > 0 ? text.Substring(0, separator).Trim() : text.Trim();
        }

        private static string? FindExistingToolTip(
            Control control,
            ToolTip currentToolTip)
        {
            for (Control? owner = control; owner != null; owner = owner.Parent)
            {
                for (Type? type = owner.GetType(); type != null; type = type.BaseType)
                {
                    FieldInfo[] fields = type.GetFields(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly);
                    foreach (FieldInfo field in fields)
                    {
                        if (!typeof(ToolTip).IsAssignableFrom(field.FieldType))
                            continue;

                        ToolTip? provider = null;
                        try
                        {
                            provider = field.GetValue(owner) as ToolTip;
                        }
                        catch
                        {
                            // Designer 또는 지연 초기화 필드는 건너뛴다.
                        }

                        if (provider == null || ReferenceEquals(provider, currentToolTip))
                            continue;

                        string existing = provider.GetToolTip(control);
                        if (!string.IsNullOrWhiteSpace(existing))
                            return existing;
                    }
                }
            }

            return null;
        }

        private sealed class Registration
        {
            private readonly Control owner;
            private readonly ToolTip toolTip;
            private readonly HashSet<Control> tracked = new HashSet<Control>();
            private bool disposed;

            internal Registration(Control owner, ToolTip toolTip)
            {
                this.owner = owner;
                this.toolTip = toolTip;
                AttachControl(owner);
            }

            private void AttachControl(Control control)
            {
                if (disposed || control.IsDisposed || !tracked.Add(control))
                    return;

                control.ControlAdded += Control_ControlAdded;
                control.Disposed += Control_Disposed;

                if (IsInteractive(control))
                {
                    string? existing = FindExistingToolTip(control, toolTip);
                    if (!string.IsNullOrWhiteSpace(existing))
                    {
                        control.AccessibleDescription = existing;
                    }
                    else
                    {
                        Set(toolTip, control, ResolveControlHelp(control));
                    }
                }

                foreach (Control child in control.Controls)
                    AttachControl(child);
            }

            private void Control_ControlAdded(object? sender, ControlEventArgs e)
            {
                AttachControl(e.Control);
            }

            private void Control_Disposed(object? sender, EventArgs e)
            {
                if (sender is not Control control)
                    return;

                if (ReferenceEquals(control, owner))
                {
                    Dispose();
                    return;
                }

                control.ControlAdded -= Control_ControlAdded;
                control.Disposed -= Control_Disposed;
                try
                {
                    toolTip.SetToolTip(control, null);
                }
                catch (ObjectDisposedException)
                {
                    // 소유 화면 종료 과정에서 ToolTip이 먼저 정리된 경우이다.
                }
                tracked.Remove(control);
            }

            private void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                foreach (Control control in tracked.ToArray())
                {
                    control.ControlAdded -= Control_ControlAdded;
                    control.Disposed -= Control_Disposed;
                }
                tracked.Clear();
                toolTip.RemoveAll();
                toolTip.Dispose();
            }
        }
    }
}
