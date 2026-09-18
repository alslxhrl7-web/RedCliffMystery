#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RedCliffMystery.Dialogue;
using RedCliffMystery.Ending;
using RedCliffMystery.Save;
using RedCliffMystery.PlayerControl;

namespace RedCliffMystery.EditorTools
{
    /// <summary>
    /// 지금까지 코드로만 존재하던 저장/엔딩/대화/최종 지목 시스템을 실제로 "플레이"해볼 수 있도록,
    /// 씬 두 개(Investigation, Ending)를 자동으로 만들어 필요한 오브젝트를 배치하고 인스펙터 참조까지
    /// 전부 연결해주는 편집기 전용 도구입니다.
    ///
    /// 아직 실제 3D 캐릭터/맵 애셋이 없기 때문에, NPC와 플레이어는 전부 기본 도형(캡슐/큐브)으로
    /// 대신합니다. 나중에 실제 아트가 준비되면 이 오브젝트들의 Mesh만 교체하면 됩니다 —
    /// 스크립트 연결(대화 데이터, 트리거 콜라이더 등)은 그대로 재사용할 수 있습니다.
    ///
    /// 메뉴: 적벽추리 > 플레이 가능한 씬 자동 생성
    ///
    /// 실행하면:
    /// 1) 대사/엔딩 데이터 애셋이 없으면 먼저 만듭니다 (DialogueDataBootstrapper.GenerateAll 호출).
    /// 2) Assets/Scenes/Investigation.unity : 플레이어 이동 + NPC 5명 + 대화 UI + 최종 지목 UI를 갖춘 메인 씬.
    /// 3) Assets/Scenes/Ending.unity : 엔딩 문구를 보여주는 결과 씬.
    /// 4) 두 씬을 Build Settings에 등록합니다 (SceneManager.LoadScene이 이름으로 씬을 찾으려면 필수).
    /// 5) Investigation 씬을 열어둔 채로 끝나므로, 바로 Play 버튼을 누르면 됩니다.
    ///
    /// 이미 같은 경로에 씬 파일이 있으면 새로 덮어씁니다. 직접 손으로 수정해둔 내용이 있다면
    /// 실행 전에 씬을 복사해두는 것을 권장합니다.
    /// </summary>
    public static class SceneBootstrapper
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const string InvestigationScenePath = ScenesFolder + "/Investigation.unity";
        private const string EndingScenePath = ScenesFolder + "/Ending.unity";

        private const string DialogueFolder = "Assets/Data/Dialogue";
        private const string EndingFolder = "Assets/Data/Endings";
        private const string CharactersFolder = "Assets/Characters";

        [MenuItem("적벽추리/플레이 가능한 씬 자동 생성")]
        public static void GenerateAll()
        {
            DialogueDataBootstrapper.GenerateAll();

            BuildInvestigationScene();
            BuildEndingScene();

            AddScenesToBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(InvestigationScenePath);

            Debug.Log("[SceneBootstrapper] Investigation.unity / Ending.unity 생성 완료. " +
                      "Investigation 씬이 열려 있으니 바로 Play를 눌러 테스트하세요.\n" +
                      "조작법: WASD/방향키 이동, NPC 근처에서 E로 대화, 안쪽 끝의 '조조에게 보고' 상자에서 E로 최종 지목, " +
                      "F5 저장/F9 불러오기, F10 강제 엔딩 판정.");
        }

        // ====================================================================
        // Investigation 씬
        // ====================================================================

        private static void BuildInvestigationScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 카메라: 캐릭터 컨트롤이 없어도 전체를 내려다볼 수 있도록 고정 위치에 배치
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 14f, -11f);
                cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            }

            // ---------------- 매니저 오브젝트 ----------------
            var saveManagerGo = new GameObject("SaveManager");
            saveManagerGo.AddComponent<SaveManager>();
            saveManagerGo.AddComponent<SaveLoadHotkeys>();

            var clueDatabase = AssetDatabase.LoadAssetAtPath<ClueDatabase>($"{DialogueFolder}/ClueDatabase.asset");

            var endingManagerGo = new GameObject("EndingManager");
            var endingManager = endingManagerGo.AddComponent<EndingManager>();
            endingManagerGo.AddComponent<EndingTestTrigger>();
            WireEndingPriorityList(endingManager);

            // ---------------- 대화 UI (Canvas) ----------------
            var canvasGo = CreateCanvas("Canvas");
            CreateEventSystem();

            GameObject dialoguePanelRoot = BuildDialoguePanel(canvasGo.transform,
                out Text speakerText, out Text lineText, out Button advanceButton,
                out GameObject topicMenuPanel, out Transform topicListContent, out Button endDialogueButton);

            Button topicButtonTemplate = CreateTemplateButton(canvasGo.transform, "TopicButtonTemplate", "화제");

            var dialogueUiGo = new GameObject("DialogueUIController");
            dialogueUiGo.transform.SetParent(canvasGo.transform, false);
            var dialogueUi = dialogueUiGo.AddComponent<DialogueUIController>();
            SetField(dialogueUi, "dialoguePanelRoot", dialoguePanelRoot);
            SetField(dialogueUi, "linePanel", dialoguePanelRoot.transform.Find("LinePanel").gameObject);
            SetField(dialogueUi, "speakerNameText", speakerText);
            SetField(dialogueUi, "lineText", lineText);
            SetField(dialogueUi, "advanceButton", advanceButton);
            SetField(dialogueUi, "topicMenuPanel", topicMenuPanel);
            SetField(dialogueUi, "topicListContent", topicListContent);
            SetField(dialogueUi, "evidenceListContent", topicListContent); // 화제/증거 버튼을 한 목록에 같이 나열
            SetField(dialogueUi, "topicButtonPrefab", topicButtonTemplate);
            SetField(dialogueUi, "endDialogueButton", endDialogueButton);

            var dialogueManagerGo = new GameObject("DialogueManager");
            var dialogueManager = dialogueManagerGo.AddComponent<DialogueManager>();
            SetField(dialogueManager, "ui", dialogueUi);
            SetField(dialogueManager, "clueDatabase", clueDatabase);

            // ---------------- 최종 지목 UI ----------------
            GameObject finalAccusationRoot = BuildFinalAccusationUI(canvasGo.transform, clueDatabase, out _);
            finalAccusationRoot.SetActive(false); // 트리거로 활성화하기 전까지는 꺼둔다

            // ---------------- 월드: 바닥 ----------------
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(2.5f, 1f, 2.5f);

            // ---------------- 월드: 플레이어 ----------------
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 1f, -6f);
            player.tag = "Player";
            var cc = player.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 1f, 0f);
            cc.height = 2f;
            cc.radius = 0.5f;
            player.AddComponent<SimplePlayerMover>();
            player.AddComponent<PlayerSaveData>();

            GameObject protagonistPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterModelPath("Protagonist.glb"));
            GameObject playerVisual;
            if (protagonistPrefab != null)
            {
                playerVisual = InstantiateGroundedModel(protagonistPrefab, player.transform);
                playerVisual.name = "Visual";
            }
            else
            {
                playerVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                playerVisual.name = "Visual";
                playerVisual.transform.SetParent(player.transform, false);
                playerVisual.transform.localPosition = new Vector3(0f, 1f, 0f);
                SetPrimitiveColor(playerVisual, new Color(0.2f, 0.6f, 1f));
            }
            foreach (var col in playerVisual.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(col); // CharacterController가 이미 있으므로 비주얼 쪽 콜라이더는 제거
            }

            // ---------------- 월드: NPC 5명 ----------------
            CreateNpc("NPC_방통", new Vector3(-4f, 1f, 2f), "Dialogue_Pangtong.asset", new Color(0.9f, 0.8f, 0.3f), "Pangtong.glb");
            CreateNpc("NPC_장간", new Vector3(-2f, 1f, 2f), "Dialogue_Jianggan.asset", new Color(0.7f, 0.6f, 0.9f), "Jianggan.glb");
            CreateNpc("NPC_두석", new Vector3(0f, 1f, 2f), "Dialogue_Dusuk.asset", new Color(0.6f, 0.6f, 0.6f), "Dusuk.glb");
            CreateNpc("NPC_설평", new Vector3(2f, 1f, 2f), "Dialogue_Seolpyeong.asset", new Color(0.9f, 0.9f, 0.9f), "Seolpyeong.glb");
            CreateNpc("NPC_유성", new Vector3(4f, 1f, 2f), "Dialogue_Yuseong.asset", new Color(0.9f, 0.4f, 0.3f), "Yuseong.glb");

            // ---------------- 월드: 최종 보고 트리거 ----------------
            var reportTrigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            reportTrigger.name = "최종보고_조조막사";
            reportTrigger.transform.position = new Vector3(0f, 0.5f, 7f);
            reportTrigger.transform.localScale = new Vector3(1.5f, 1f, 1.5f);
            SetPrimitiveColor(reportTrigger, new Color(0.8f, 0.2f, 0.2f));
            var reportCollider = reportTrigger.GetComponent<Collider>();
            reportCollider.isTrigger = true;
            var reportTriggerComp = reportTrigger.AddComponent<FinalAccusationTrigger>();
            SetField(reportTriggerComp, "finalAccusationRoot", finalAccusationRoot);

            EditorSceneManager.MarkSceneDirty(scene);
            EnsureFolder(ScenesFolder);
            EditorSceneManager.SaveScene(scene, InvestigationScenePath);
        }

        private static void WireEndingPriorityList(EndingManager endingManager)
        {
            var list = new List<Object>();
            foreach (var id in DialogueDataBootstrapper.EndingPriorityOrder)
            {
                var asset = AssetDatabase.LoadAssetAtPath<EndingDefinition>(DialogueDataBootstrapper.EndingAssetPath(id));
                if (asset == null)
                {
                    Debug.LogWarning($"[SceneBootstrapper] 엔딩 애셋을 찾지 못했습니다: {id}");
                    continue;
                }
                list.Add(asset);
            }
            SetFieldList(endingManager, "endingsInPriorityOrder", list);
        }

        private static void CreateNpc(string name, Vector3 position, string dialogueAssetFileName, Color color, string modelFileName = null)
        {
            GameObject modelPrefab = !string.IsNullOrEmpty(modelFileName)
                ? AssetDatabase.LoadAssetAtPath<GameObject>(CharacterModelPath(modelFileName))
                : null;

            GameObject npc;
            if (modelPrefab != null)
            {
                // 실제 3D 캐릭터 모델(Rig + Animation 포함 glb) 사용.
                // 콜라이더가 없는 모델이 대부분이므로 상호작용용 트리거 콜라이더를 별도로 붙인다.
                npc = new GameObject(name);
                npc.transform.position = new Vector3(position.x, 0f, position.z); // 바닥(y=0) 기준

                InstantiateGroundedModel(modelPrefab, npc.transform);

                var capsuleCollider = npc.AddComponent<CapsuleCollider>();
                capsuleCollider.isTrigger = true;
                capsuleCollider.center = new Vector3(0f, 1f, 0f);
                capsuleCollider.height = 2f;
                capsuleCollider.radius = 0.5f;
            }
            else
            {
                // 모델 애셋을 못 찾았을 때(글TF 임포트 전 등) 기존처럼 캡슐로 대신한다.
                npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                npc.name = name;
                npc.transform.position = position;
                SetPrimitiveColor(npc, color);

                var collider = npc.GetComponent<Collider>();
                collider.isTrigger = true;
            }

            var data = AssetDatabase.LoadAssetAtPath<CharacterDialogueData>(DialogueDataBootstrapper.DialogueAssetPath(dialogueAssetFileName));
            if (data == null)
            {
                Debug.LogWarning($"[SceneBootstrapper] 대화 데이터 애셋을 찾지 못했습니다: {dialogueAssetFileName}");
            }

            var interactable = npc.AddComponent<NPCInteractable>();
            SetField(interactable, "dialogueData", data);
        }

        /// <summary>Assets/Characters 폴더 안의 캐릭터 모델(glb) 애셋 경로를 만든다.</summary>
        private static string CharacterModelPath(string fileName) => $"{CharactersFolder}/{fileName}";

        /// <summary>
        /// 캐릭터 모델 프리팹을 parent 아래에 배치하고, 렌더러 바운드를 계산해 발바닥이 월드 y=0(바닥)에
        /// 닿도록 높이를 보정한다. glb 내보내기 시 피벗 위치가 통일되어 있지 않아도 안전하게 동작한다.
        /// </summary>
        private static GameObject InstantiateGroundedModel(GameObject prefab, Transform parent)
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            var renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

                const float groundWorldY = 0f;
                visual.transform.position += new Vector3(0f, groundWorldY - bounds.min.y, 0f);
            }

            return visual;
        }

        // ====================================================================
        // 대화 패널 (Canvas 하위)
        // ====================================================================

        private static GameObject BuildDialoguePanel(Transform canvasTransform,
            out Text speakerText, out Text lineText, out Button advanceButton,
            out GameObject topicMenuPanel, out Transform topicListContent, out Button endDialogueButton)
        {
            var root = CreateStretchedPanel(canvasTransform, "DialoguePanelRoot", new Color(0f, 0f, 0f, 0f));

            // 대사 한 줄 표시 패널 (화면 하단)
            var linePanel = CreateStretchedPanel(root.transform, "LinePanel", new Color(0f, 0f, 0f, 0f));
            var lineRt = linePanel.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0f, 0f);
            lineRt.anchorMax = new Vector2(1f, 0.3f);
            lineRt.offsetMin = new Vector2(20f, 20f);
            lineRt.offsetMax = new Vector2(-20f, 0f);
            linePanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            speakerText = CreateText(linePanel.transform, "SpeakerNameText", "", 22, TextAnchor.UpperLeft, Color.yellow);
            var speakerRt = speakerText.GetComponent<RectTransform>();
            speakerRt.anchorMin = new Vector2(0f, 0.7f);
            speakerRt.anchorMax = new Vector2(1f, 1f);
            speakerRt.offsetMin = new Vector2(16f, 0f);
            speakerRt.offsetMax = new Vector2(-16f, -8f);

            lineText = CreateText(linePanel.transform, "LineText", "", 20, TextAnchor.UpperLeft, Color.white);
            var lineTextRt = lineText.GetComponent<RectTransform>();
            lineTextRt.anchorMin = new Vector2(0f, 0f);
            lineTextRt.anchorMax = new Vector2(1f, 0.7f);
            lineTextRt.offsetMin = new Vector2(16f, 8f);
            lineTextRt.offsetMax = new Vector2(-16f, -4f);

            // 대사창 전체를 덮는 투명 버튼 -> 클릭하면 다음 대사
            var advanceGo = new GameObject("AdvanceButton", typeof(RectTransform), typeof(Image), typeof(Button));
            advanceGo.transform.SetParent(linePanel.transform, false);
            StretchFull(advanceGo.GetComponent<RectTransform>());
            var advanceImage = advanceGo.GetComponent<Image>();
            advanceImage.color = new Color(0f, 0f, 0f, 0f);
            advanceButton = advanceGo.GetComponent<Button>();
            advanceButton.targetGraphic = advanceImage;

            // 화제/증거 목록 패널
            topicMenuPanel = CreateStretchedPanel(root.transform, "TopicMenuPanel", new Color(0f, 0f, 0f, 0f));
            var topicRt = topicMenuPanel.GetComponent<RectTransform>();
            topicRt.anchorMin = new Vector2(0.55f, 0.05f);
            topicRt.anchorMax = new Vector2(0.98f, 0.95f);
            topicRt.offsetMin = Vector2.zero;
            topicRt.offsetMax = Vector2.zero;
            topicMenuPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var listGo = new GameObject("TopicListContent", typeof(RectTransform));
            listGo.transform.SetParent(topicMenuPanel.transform, false);
            var listRt = listGo.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0f, 0.15f);
            listRt.anchorMax = new Vector2(1f, 1f);
            listRt.offsetMin = new Vector2(10f, 0f);
            listRt.offsetMax = new Vector2(-10f, -10f);
            var vlg = listGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            listGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            topicListContent = listGo.transform;

            var endBtnGo = CreateButtonInternal(topicMenuPanel.transform, "EndDialogueButton", "대화 종료");
            var endBtnRt = endBtnGo.GetComponent<RectTransform>();
            endBtnRt.anchorMin = new Vector2(0f, 0f);
            endBtnRt.anchorMax = new Vector2(1f, 0.12f);
            endBtnRt.offsetMin = new Vector2(10f, 10f);
            endBtnRt.offsetMax = new Vector2(-10f, 0f);
            endDialogueButton = endBtnGo.GetComponent<Button>();

            root.SetActive(false);
            return root;
        }

        // ====================================================================
        // 최종 지목 UI (Canvas 하위)
        // ====================================================================

        private static GameObject BuildFinalAccusationUI(Transform canvasTransform, ClueDatabase clueDatabase, out FinalAccusationController controller)
        {
            var root = new GameObject("FinalAccusationRoot", typeof(RectTransform));
            root.transform.SetParent(canvasTransform, false);
            StretchFull(root.GetComponent<RectTransform>());

            controller = root.AddComponent<FinalAccusationController>();

            var dialogueSet = AssetDatabase.LoadAssetAtPath<FinalAccusationDialogueSet>($"{DialogueFolder}/FinalAccusationDialogueSet.asset");
            SetField(controller, "dialogueSet", dialogueSet);
            SetField(controller, "clueDatabase", clueDatabase);
            SetField(controller, "trueCulpritId", "seolpyeong");
            SetField(controller, "minimumEvidenceForConviction", 3);

            // ---- 대상 선택 패널 ----
            var targetPanel = CreateStretchedPanel(root.transform, "TargetSelectionPanel", new Color(0f, 0f, 0f, 0.6f));

            var selectedText = CreateText(targetPanel.transform, "SelectedTargetText", "지목 대상: (선택 안 됨)", 20, TextAnchor.MiddleCenter, Color.white);
            var selRt = selectedText.GetComponent<RectTransform>();
            selRt.anchorMin = new Vector2(0.2f, 0.75f);
            selRt.anchorMax = new Vector2(0.8f, 0.9f);
            selRt.offsetMin = Vector2.zero;
            selRt.offsetMax = Vector2.zero;

            Button pangtongBtn = CreateLabeledButton(targetPanel.transform, "PangtongButton", "방통", 0.10f, 0.55f);
            Button jianggangBtn = CreateLabeledButton(targetPanel.transform, "JianggangButton", "장간", 0.25f, 0.55f);
            Button dusukBtn = CreateLabeledButton(targetPanel.transform, "DusukButton", "두석", 0.40f, 0.55f);
            Button seolpyeongBtn = CreateLabeledButton(targetPanel.transform, "SeolpyeongButton", "설평", 0.55f, 0.55f);
            Button caimaoBtn = CreateLabeledButton(targetPanel.transform, "CaimaoButton", "채모·장윤", 0.70f, 0.55f);

            Button confirmBtn = CreateLabeledButton(targetPanel.transform, "ConfirmButton", "확정", 0.40f, 0.30f);

            SetField(controller, "targetSelectionPanel", targetPanel);
            SetField(controller, "pangtongButton", pangtongBtn);
            SetField(controller, "jianggangButton", jianggangBtn);
            SetField(controller, "dusukButton", dusukBtn);
            SetField(controller, "seolpyeongButton", seolpyeongBtn);
            SetField(controller, "caimaoButton", caimaoBtn);
            SetField(controller, "confirmButton", confirmBtn);
            SetField(controller, "selectedTargetText", selectedText);

            targetPanel.SetActive(false);

            // ---- 증거 제시 패널 ----
            var evidencePanel = CreateStretchedPanel(root.transform, "EvidenceSelectionPanel", new Color(0f, 0f, 0f, 0.6f));

            var evidenceListGo = new GameObject("EvidenceToggleListContent", typeof(RectTransform));
            evidenceListGo.transform.SetParent(evidencePanel.transform, false);
            var evListRt = evidenceListGo.GetComponent<RectTransform>();
            evListRt.anchorMin = new Vector2(0.25f, 0.25f);
            evListRt.anchorMax = new Vector2(0.75f, 0.9f);
            evListRt.offsetMin = Vector2.zero;
            evListRt.offsetMax = Vector2.zero;
            var evVlg = evidenceListGo.AddComponent<VerticalLayoutGroup>();
            evVlg.spacing = 4f;
            evVlg.childControlHeight = false;
            evVlg.childControlWidth = true;
            evVlg.childForceExpandWidth = true;
            evVlg.childForceExpandHeight = false;
            evidenceListGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Toggle toggleTemplate = CreateTemplateToggle(evidencePanel.transform, "EvidenceTogglePrefab");

            Button submitBtn = CreateLabeledButton(evidencePanel.transform, "SubmitEvidenceButton", "증거 제시 확정", 0.40f, 0.08f);

            SetField(controller, "evidenceSelectionPanel", evidencePanel);
            SetField(controller, "evidenceToggleListContent", evidenceListGo.transform);
            SetField(controller, "evidenceTogglePrefab", toggleTemplate);
            SetField(controller, "submitEvidenceButton", submitBtn);

            evidencePanel.SetActive(false);

            return root;
        }

        // ====================================================================
        // Ending 씬
        // ====================================================================

        private static void BuildEndingScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var canvasGo = CreateCanvas("Canvas");
            CreateEventSystem();

            var panel = CreateStretchedPanel(canvasGo.transform, "EndingPanel", new Color(0f, 0f, 0f, 0.85f));

            var titleText = CreateText(panel.transform, "TitleText", "", 34, TextAnchor.MiddleCenter, Color.yellow);
            var titleRt = titleText.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.1f, 0.75f);
            titleRt.anchorMax = new Vector2(0.9f, 0.9f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            var bodyText = CreateText(panel.transform, "BodyText", "", 20, TextAnchor.UpperLeft, Color.white);
            var bodyRt = bodyText.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0.1f, 0.35f);
            bodyRt.anchorMax = new Vector2(0.9f, 0.72f);
            bodyRt.offsetMin = Vector2.zero;
            bodyRt.offsetMax = Vector2.zero;

            var backgroundGo = new GameObject("BackgroundImage", typeof(RectTransform), typeof(Image));
            backgroundGo.transform.SetParent(panel.transform, false);
            backgroundGo.transform.SetAsFirstSibling();
            StretchFull(backgroundGo.GetComponent<RectTransform>());
            var backgroundImage = backgroundGo.GetComponent<Image>();
            backgroundImage.color = new Color(0f, 0f, 0f, 0f);
            backgroundImage.enabled = false;

            Button restartBtn = CreateLabeledButton(panel.transform, "RestartButton", "다시 조사하기", 0.20f, 0.15f);
            Button quitBtn = CreateLabeledButton(panel.transform, "QuitToTitleButton", "타이틀로", 0.60f, 0.15f);

            var controllerGo = new GameObject("EndingUIController");
            var controller = controllerGo.AddComponent<EndingUIController>();
            SetField(controller, "titleText", titleText);
            SetField(controller, "bodyText", bodyText);
            SetField(controller, "backgroundImage", backgroundImage);
            SetField(controller, "restartButton", restartBtn);
            SetField(controller, "quitToTitleButton", quitBtn);
            SetField(controller, "gameplaySceneName", "Investigation");
            SetField(controller, "titleSceneName", "Investigation");

            var trueEnding = AssetDatabase.LoadAssetAtPath<EndingDefinition>(DialogueDataBootstrapper.EndingAssetPath("true_ending"));
            SetField(controller, "previewEndingForStandaloneTesting", trueEnding);

            EditorSceneManager.MarkSceneDirty(scene);
            EnsureFolder(ScenesFolder);
            EditorSceneManager.SaveScene(scene, EndingScenePath);
        }

        // ====================================================================
        // Build Settings
        // ====================================================================

        private static void AddScenesToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            AddOrUpdateScene(scenes, InvestigationScenePath);
            AddOrUpdateScene(scenes, EndingScenePath);

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddOrUpdateScene(List<EditorBuildSettingsScene> scenes, string path)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == path)
                {
                    scenes[i] = new EditorBuildSettingsScene(path, true);
                    return;
                }
            }
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        // ====================================================================
        // UI 생성 헬퍼
        // ====================================================================

        private static GameObject CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return go;
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = go.GetComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private static GameObject CreateStretchedPanel(Transform parent, string name, Color backgroundColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            StretchFull(go.GetComponent<RectTransform>());
            go.GetComponent<Image>().color = backgroundColor;
            return go;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.text = content;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static GameObject CreateButtonInternal(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.9f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var labelText = CreateText(go.transform, "Text", label, 18, TextAnchor.MiddleCenter, Color.black);
            StretchFull(labelText.GetComponent<RectTransform>());

            return go;
        }

        /// <summary>화면 비율(0~1) 기준 위치에 고정 크기 버튼을 만듭니다.</summary>
        private static Button CreateLabeledButton(Transform parent, string name, string label, float anchorX, float anchorY)
        {
            var go = CreateButtonInternal(parent, name, label);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorX, anchorY);
            rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(220f, 50f);
            rt.anchoredPosition = Vector2.zero;
            return go.GetComponent<Button>();
        }

        /// <summary>Instantiate로 복제해 쓰는 버튼 템플릿. 항상 비활성 상태로 둡니다.</summary>
        private static Button CreateTemplateButton(Transform parent, string name, string placeholderLabel)
        {
            var go = CreateButtonInternal(parent, name, placeholderLabel);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 44f);
            go.SetActive(false);
            return go.GetComponent<Button>();
        }

        /// <summary>Instantiate로 복제해 쓰는 체크박스 템플릿. 항상 비활성 상태로 둡니다.</summary>
        private static Toggle CreateTemplateToggle(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Toggle));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 32f);

            var bgImage = go.GetComponent<Image>();
            bgImage.color = new Color(1f, 1f, 1f, 0.9f);

            var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkGo.transform.SetParent(go.transform, false);
            var checkRt = checkGo.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0f, 0.2f);
            checkRt.anchorMax = new Vector2(0.12f, 0.8f);
            checkRt.offsetMin = Vector2.zero;
            checkRt.offsetMax = Vector2.zero;
            var checkImage = checkGo.GetComponent<Image>();
            checkImage.color = new Color(0.1f, 0.6f, 0.1f, 1f);

            var labelText = CreateText(go.transform, "Text", "(단서)", 16, TextAnchor.MiddleLeft, Color.black);
            var labelRt = labelText.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.15f, 0f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = false;

            go.SetActive(false);
            return toggle;
        }

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return font;
        }

        private static void SetPrimitiveColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null)
            {
                // 셰이더를 하나도 못 찾으면(비정상적인 경우) 기본 머티리얼 색을 그대로 둔다.
                Debug.LogWarning("[SceneBootstrapper] URP/Standard 셰이더를 찾지 못해 기본 색상을 적용하지 못했습니다.");
                return;
            }

            var material = new Material(shader) { color = color };
            renderer.sharedMaterial = material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        // ====================================================================
        // private [SerializeField] 값을 편집기 스크립트에서 안전하게 설정하는 헬퍼
        // ====================================================================

        private static void SetField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[SceneBootstrapper] {target.GetType().Name}에서 필드 '{fieldName}'을(를) 찾지 못했습니다.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetField(Object target, string fieldName, string value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[SceneBootstrapper] {target.GetType().Name}에서 필드 '{fieldName}'을(를) 찾지 못했습니다.");
                return;
            }
            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetField(Object target, string fieldName, int value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[SceneBootstrapper] {target.GetType().Name}에서 필드 '{fieldName}'을(를) 찾지 못했습니다.");
                return;
            }
            prop.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFieldList(Object target, string fieldName, List<Object> values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[SceneBootstrapper] {target.GetType().Name}에서 필드 '{fieldName}'을(를) 찾지 못했습니다.");
                return;
            }
            prop.ClearArray();
            for (int i = 0; i < values.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
