#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RedCliffMystery.Dialogue;

namespace RedCliffMystery.EditorTools
{
    /// <summary>
    /// 사건기획서_v1.md 10절에 작성된 대사를 CharacterDialogueData / ClueDatabase 애셋으로
    /// 한 번에 만들어주는 편집기 전용 도구.
    ///
    /// 메뉴: 적벽추리 > 대사 데이터 자동 생성
    ///
    /// 이미 같은 경로에 애셋이 있으면 그 필드를 덮어씁니다(새로 만들지 않고 기존 애셋을 재사용합니다).
    /// 인스펙터에서 직접 수정해둔 내용이 있다면 실행 전에 백업하거나, 이 스크립트의 값만 참고해서
    /// 손으로 옮기는 방법도 있습니다.
    /// </summary>
    public static class DialogueDataBootstrapper
    {
        private const string OutputFolder = "Assets/Data/Dialogue";

        [MenuItem("적벽추리/대사 데이터 자동 생성")]
        public static void GenerateAll()
        {
            EnsureFolder(OutputFolder);

            CreateClueDatabase();
            CreatePangtong();
            CreateJianggan();
            CreateDusuk();
            CreateSeolpyeong();
            CreateYuseong();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DialogueDataBootstrapper] 대사 데이터 생성 완료: " + OutputFolder);
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

        private static T CreateOrLoadAsset<T>(string assetPath) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static DialogueLine L(string speaker, string text) => new DialogueLine(speaker, text);

        private static List<DialogueCondition> Cond(DialogueConditionType type, string key) =>
            new List<DialogueCondition> { new DialogueCondition { type = type, key = key } };

        // ------------------------------------------------------------------
        // 단서 이름 데이터베이스 (사건기획서 9절과 대응)
        // ------------------------------------------------------------------
        private static void CreateClueDatabase()
        {
            var db = CreateOrLoadAsset<ClueDatabase>($"{OutputFolder}/ClueDatabase.asset");
            db.clues = new List<ClueInfo>
            {
                new ClueInfo{ clueId = "clue_household_registry_no_record", displayName = "호적 대장 사본" },
                new ClueInfo{ clueId = "clue_wu_secret_letter_true_name", displayName = "오나라 밀서" },
                new ClueInfo{ clueId = "clue_pangtong_night_visitor", displayName = "한밤중 목격담" },
                new ClueInfo{ clueId = "clue_pangtong_zhouyu_rumor", displayName = "방통과 주유의 옛 인연 소문" },
                new ClueInfo{ clueId = "clue_pangtong_alibi_witness", displayName = "방통의 알리바이 증언" },
                new ClueInfo{ clueId = "clue_jianggan_forged_letter_habit", displayName = "장간의 과거 전력" },
                new ClueInfo{ clueId = "clue_jianggan_suspicious_visit", displayName = "문서고 방문 기록(장간)" },
                new ClueInfo{ clueId = "clue_jianggan_personal_letter", displayName = "장간이 맡긴 개인 서신" },
                new ClueInfo{ clueId = "clue_dusuk_gambling_debt", displayName = "두석의 도박 빚" },
                new ClueInfo{ clueId = "clue_dusuk_false_alibi", displayName = "두석의 거짓 진술" },
                new ClueInfo{ clueId = "clue_dusuk_confession", displayName = "두석의 실토" },
                new ClueInfo{ clueId = "clue_document_hall_access_log", displayName = "문서고 출입 기록부" },
                new ClueInfo{ clueId = "clue_yuseong_leaked_info", displayName = "유성의 정보 유출 정황" },
            };
            EditorUtility.SetDirty(db);
        }

        // ------------------------------------------------------------------
        // 방통
        // ------------------------------------------------------------------
        private static void CreatePangtong()
        {
            var d = CreateOrLoadAsset<CharacterDialogueData>($"{OutputFolder}/Dialogue_Pangtong.asset");
            d.characterId = "pangtong";
            d.displayName = "방통";

            d.introLines = new List<DialogueLine>
            {
                L("방통", "음? 뭔가 캐물으러 온 모양이군. 그 문서 얘기겠지."),
                L("방통", "나도 그 방비책의 중요성은 잘 알고 있다. 도움이 될 일이라면 뭐든 묻게."),
            };

            var topicA = new DialogueTopic
            {
                topicId = "pt_lianhuan",
                buttonLabel = "연환계에 대하여",
                lines = new List<DialogueLine>
                {
                    L(null, "연환계는 승상의 뜻입니까, 아니면 공의 건의였습니까?"),
                    L("방통", "내가 올린 계책이다. 배멀미로 쓰러지는 병사들을 보고만 있을 수는 없었으니까."),
                    L("방통", "...한데 왜 그런 걸 묻나? 설마 이 계책 자체를 의심하는 겐가?"),
                },
            };

            var topicB = new DialogueTopic
            {
                topicId = "pt_night",
                buttonLabel = "그날 밤 행적",
                lines = new List<DialogueLine> { L("방통", "그날 밤? 딱히 특별한 일은 없었다.") },
            };

            var topicC = new DialogueTopic
            {
                topicId = "pt_zhouyu",
                buttonLabel = "주유와의 인연",
                visibilityConditions = Cond(DialogueConditionType.ClueCollected, "clue_pangtong_zhouyu_rumor"),
                lines = new List<DialogueLine>
                {
                    L(null, "예전에 주유 도독과 가까우셨다는 소문이 있던데."),
                    L("방통", "...옛일이다. 그게 지금 이 사건과 무슨 상관인가?"),
                    L("방통", "옛 인연이 있다고 해서 전부 첩자 취급을 한다면, 이 진영에 남아날 사람이 몇이나 되겠나."),
                },
            };

            d.topics = new List<DialogueTopic> { topicA, topicB, topicC };

            d.evidencePresentations = new List<EvidencePresentation>
            {
                new EvidencePresentation
                {
                    requiredClueId = "clue_pangtong_night_visitor",
                    reactionLines = new List<DialogueLine>
                    {
                        L(null, "그날 밤, 강변 쪽으로 혼자 나가시는 걸 봤다는 자가 있습니다."),
                        L("방통", "...잠이 오지 않아 바람을 좀 쐈을 뿐이다. 그게 죄가 되나?"),
                        L("방통", "믿기지 않는다면, 그 뒤에 곽 참모와 함께 있었으니 그에게 물어보게. 밤새 병법을 논했으니."),
                    },
                    effects = new List<DialogueEffect>
                    {
                        new DialogueEffect{ type = DialogueEffectType.SetFlagTrue, key = "pangtong_alibi_lead" },
                    },
                },
            };

            EditorUtility.SetDirty(d);
        }

        // ------------------------------------------------------------------
        // 장간
        // ------------------------------------------------------------------
        private static void CreateJianggan()
        {
            var d = CreateOrLoadAsset<CharacterDialogueData>($"{OutputFolder}/Dialogue_Jianggan.asset");
            d.characterId = "jianggan";
            d.displayName = "장간";

            d.introLines = new List<DialogueLine>
            {
                L("장간", "또 나를 의심하러 온 겐가... 그 일 이후로 다들 나만 보면 그런 눈으로 보더군."),
            };

            var topicA = new DialogueTopic
            {
                topicId = "jg_past",
                buttonLabel = "과거 위조편지 사건",
                lines = new List<DialogueLine>
                {
                    L(null, "예전에 채모, 장윤 두 사람을 모함에 빠뜨린 편지 사건이 있었다고 들었습니다."),
                    L("장간", "...그건 내가 속아서 벌어진 일이다. 나 역시 이용당한 것뿐이야."),
                    L("장간", "그 일로 승상께 큰 실망을 안겨드렸지. 이번엔... 이번엔 절대 그런 실수를 하지 않을 것이다."),
                },
            };

            var topicC = new DialogueTopic
            {
                topicId = "jg_letter",
                buttonLabel = "개인 서신",
                visibilityConditions = Cond(DialogueConditionType.FlagTrue, "jianggan_pressed"),
                lines = new List<DialogueLine>
                {
                    L("장간", "(한숨 쉬며 품에서 편지를 꺼낸다) ...부끄럽지만, 고향에 있는 여인에게 보낼 서신을 문서고 관리인에게 몰래 맡기려던 것뿐이었다. 인편이 마땅치 않아서..."),
                    L("장간", "제발 이 얘기는... 다른 이들에게는 하지 말아주게."),
                },
                effectsOnFirstView = new List<DialogueEffect>
                {
                    new DialogueEffect{ type = DialogueEffectType.MarkClueCollected, key = "clue_jianggan_personal_letter" },
                    new DialogueEffect{ type = DialogueEffectType.SetFlagTrue, key = "jianggan_cleared" },
                },
            };

            d.topics = new List<DialogueTopic> { topicA, topicC };

            d.evidencePresentations = new List<EvidencePresentation>
            {
                new EvidencePresentation
                {
                    requiredClueId = "clue_jianggan_suspicious_visit",
                    reactionLines = new List<DialogueLine>
                    {
                        L(null, "사건 당일, 문서고를 방문한 기록이 남아있던데요."),
                        L("장간", "(당황) 그, 그건... 아니 그게..."),
                        L("장간", "말하지 않으려 했는데... 사실 개인적인 용무였다."),
                    },
                    effects = new List<DialogueEffect>
                    {
                        new DialogueEffect{ type = DialogueEffectType.SetFlagTrue, key = "jianggan_pressed" },
                    },
                },
            };

            EditorUtility.SetDirty(d);
        }

        // ------------------------------------------------------------------
        // 두석
        // ------------------------------------------------------------------
        private static void CreateDusuk()
        {
            var d = CreateOrLoadAsset<CharacterDialogueData>($"{OutputFolder}/Dialogue_Dusuk.asset");
            d.characterId = "dusuk";
            d.displayName = "두석";

            d.introLines = new List<DialogueLine>
            {
                L("두석", "(부동자세, 긴장한 기색) 충성. 그날 밤 근무에 대해 물으실 거라면... 이상 없었습니다."),
            };

            var topicA = new DialogueTopic
            {
                topicId = "ds_night",
                buttonLabel = "그날 밤 경비 상황",
                lines = new List<DialogueLine>
                {
                    L(null, "그날 밤 문서고 앞을 계속 지키고 있었나?"),
                    L("두석", "예, 예. 한시도 자리를 비우지 않았습니다."),
                },
            };

            var topicB = new DialogueTopic
            {
                topicId = "ds_gambling",
                buttonLabel = "도박 빚 소문",
                visibilityConditions = Cond(DialogueConditionType.ClueCollected, "clue_dusuk_gambling_debt"),
                lines = new List<DialogueLine>
                {
                    L(null, "요즘 도박 빚 때문에 곤란하다는 얘기를 들었는데."),
                    L("두석", "(표정 굳음) ...누가 그런 헛소문을! 근무와는 상관없는 일입니다."),
                },
            };

            var topicD = new DialogueTopic
            {
                topicId = "ds_confession",
                buttonLabel = "실토",
                visibilityConditions = Cond(DialogueConditionType.FlagTrue, "dusuk_pressed"),
                lines = new List<DialogueLine>
                {
                    L("두석", "잠깐, 아주 잠깐... 반 시진 정도 자리를 비웠습니다. 노름판에 껴 있느라... 그것 때문에 근무 일지를 손봤습니다."),
                    L("두석", "문서 도난과는 전혀 상관없는 일입니다! 그것만은 믿어주십시오..."),
                },
                effectsOnFirstView = new List<DialogueEffect>
                {
                    new DialogueEffect{ type = DialogueEffectType.MarkClueCollected, key = "clue_dusuk_confession" },
                    new DialogueEffect{ type = DialogueEffectType.SetFlagTrue, key = "dusuk_cleared" },
                },
            };

            d.topics = new List<DialogueTopic> { topicA, topicB, topicD };

            d.evidencePresentations = new List<EvidencePresentation>
            {
                new EvidencePresentation
                {
                    requiredClueId = "clue_dusuk_false_alibi",
                    reactionLines = new List<DialogueLine>
                    {
                        L(null, "근무 일지와 다른 병사들의 증언이 서로 어긋나던데. 정말 한시도 자리를 비우지 않았나?"),
                        L("두석", "(침묵) ..."),
                        L("두석", "...죄송합니다. 사실은..."),
                    },
                    effects = new List<DialogueEffect>
                    {
                        new DialogueEffect{ type = DialogueEffectType.SetFlagTrue, key = "dusuk_pressed" },
                    },
                },
            };

            EditorUtility.SetDirty(d);
        }

        // ------------------------------------------------------------------
        // 설평 (목연)
        // ------------------------------------------------------------------
        private static void CreateSeolpyeong()
        {
            var d = CreateOrLoadAsset<CharacterDialogueData>($"{OutputFolder}/Dialogue_Seolpyeong.asset");
            d.characterId = "seolpyeong";
            d.displayName = "설평";

            d.introLines = new List<DialogueLine>
            {
                L("설평", "아이고, 오셨습니까. 문서고 일이라면 제가 제일 잘 알지요. 뭐든 물어보십시오."),
                L("설평", "이런 일이 생겨서 저도 밤잠을 설칩니다. 얼른 범인을 잡아야 할 텐데요..."),
            };

            var topicA = new DialogueTopic
            {
                topicId = "sp_archive",
                buttonLabel = "문서고 관리",
                lines = new List<DialogueLine>
                {
                    L("설평", "그 문서는 제가 매일 점검하던 것입니다. 그날도 분명 제자리에 있었는데..."),
                },
            };

            var topicB = new DialogueTopic
            {
                topicId = "sp_hometown",
                buttonLabel = "고향/이름",
                lines = new List<DialogueLine>
                {
                    L(null, "그러고 보니 설 서기는 고향이 어디였소?"),
                    L("설평", "(웃으며) 저 같은 미천한 사람 고향 이야기가 뭐 그리 중요하겠습니까. 그냥 흔한 시골 마을입니다."),
                },
            };

            // 최종 대면: 핵심 단서 2개(호적 대장 + 오나라 밀서)를 모두 모아야 열림 (AND 조건)
            var topicFinal = new DialogueTopic
            {
                topicId = "sp_final_confrontation",
                buttonLabel = "[결정적 심문] 정체를 추궁한다",
                hideAfterFirstView = true,
                visibilityConditions = new List<DialogueCondition>
                {
                    new DialogueCondition{ type = DialogueConditionType.ClueCollected, key = "clue_household_registry_no_record" },
                    new DialogueCondition{ type = DialogueConditionType.ClueCollected, key = "clue_wu_secret_letter_true_name" },
                },
                lines = new List<DialogueLine>
                {
                    L(null, "설평... 그대의 고향이라는 마을 호적을 다 뒤져봤네. 그런데 그런 이름으로 태어난 자가 없더군."),
                    L("설평", "(표정이 순간 굳었다가 다시 웃는다) 호적이야 전란 통에 소실되는 경우가 워낙 많지 않습니까. 그런 걸로 사람을 의심하시면..."),
                    L(null, "그럼 이건 어떻게 설명하겠나. 오나라와 오간 서신 속에 '목연'이라는 이름이 적혀 있더군."),
                    L("설평", "(긴 침묵. 웃음기가 완전히 사라진다) ...목연. 그 이름을 부르는 것도 참 오랜만이군요."),
                    L("설평", "설평이라는 이름은... 그저 아무도 눈여겨보지 않을 이름이 필요해서 골랐을 뿐입니다. 너무 흔하고, 너무 평범해서 아무도 의심하지 않을 이름."),
                    L("설평", "화목함을 퍼뜨리라고 지어주신 이름인데... 결국은 이런 식으로 소식을 퍼뜨리는 자가 되었군요. 아이러니하지 않습니까?"),
                    L("목연", "이제 와서 부인하지는 않겠습니다. 예, 제가 그 문서를 넘겼습니다. 강동을 위해서."),
                },
                effectsOnFirstView = new List<DialogueEffect>
                {
                    new DialogueEffect{ type = DialogueEffectType.SetFlagTrue, key = "seolpyeong_exposed" },
                },
            };

            d.topics = new List<DialogueTopic> { topicA, topicB, topicFinal };
            d.evidencePresentations = new List<EvidencePresentation>();

            EditorUtility.SetDirty(d);
        }

        // ------------------------------------------------------------------
        // 유성
        // ------------------------------------------------------------------
        private static void CreateYuseong()
        {
            var d = CreateOrLoadAsset<CharacterDialogueData>($"{OutputFolder}/Dialogue_Yuseong.asset");
            d.characterId = "yuseong";
            d.displayName = "유성";

            d.introLines = new List<DialogueLine>
            {
                L("유성", "어이, 자네도 그 사건 때문에 정신없나 보군. 나도 마찬가지야."),
            };

            var topicA = new DialogueTopic
            {
                topicId = "ys_rivalry",
                buttonLabel = "견제/경쟁",
                lines = new List<DialogueLine>
                {
                    L("유성", "너무 티 나게 열심히 하지는 말게. 공을 세우고 싶은 마음이야 이해하지만... 나도 지고 싶지는 않거든."),
                },
            };

            var topicB = new DialogueTopic
            {
                topicId = "ys_leak",
                buttonLabel = "정보 유출 추궁",
                visibilityConditions = Cond(DialogueConditionType.ClueCollected, "clue_yuseong_leaked_info"),
                lines = new List<DialogueLine>
                {
                    L(null, "자네, 수사 정보를 다른 데 흘리고 다닌다는 얘기가 있던데."),
                    L("유성", "(태연하게) 글쎄, 누가 그런 말을 하던가? ...뭐, 승상의 다른 측근들도 이 사건에 관심이 많다는 것뿐이지. 딱히 숨길 일도 아니지 않나?"),
                },
            };

            d.topics = new List<DialogueTopic> { topicA, topicB };
            d.evidencePresentations = new List<EvidencePresentation>();

            EditorUtility.SetDirty(d);
        }
    }
}
#endif
