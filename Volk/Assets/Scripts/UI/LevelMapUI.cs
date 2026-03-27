using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using Volk.Core;
using Volk.Story;

namespace Volk.UI
{
    public class LevelMapUI : MonoBehaviour
    {
        [Header("Map")]
        public ScrollRect scrollView;
        public Transform nodeContainer;
        public GameObject normalNodePrefab;
        public GameObject bossNodePrefab;

        [Header("Detail Panel")]
        public GameObject detailPanel;
        public TextMeshProUGUI detailTitle;
        public TextMeshProUGUI detailDescription;
        public TextMeshProUGUI detailDifficulty;
        public TextMeshProUGUI detailReward;
        public Image detailEnemyPortrait;
        public Image[] starImages;
        public Button playButton;
        public Button closeDetailButton;

        [Header("Navigation")]
        public Button backButton;
        public TextMeshProUGUI headerTitle;
        public Image backgroundImage;

        [Header("Grade Colors")]
        public Color starActive = new Color(1f, 0.84f, 0f);
        public Color starInactive = new Color(0.2f, 0.2f, 0.3f);

        [Header("Node Colors")]
        public Color nodeCompleted = new Color(0f, 0.83f, 0.42f);
        public Color nodeCurrent = new Color(0.91f, 0.27f, 0.38f);
        public Color nodeLocked = new Color(0.2f, 0.2f, 0.3f);
        public Color nodeBoss = new Color(1f, 0.84f, 0f);

        private ChapterData selectedChapter;
        private int selectedIndex;

        void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        void Start()
        {
            if (backgroundImage) backgroundImage.color = VTheme.Background;
            if (headerTitle) { headerTitle.text = "HIKAYE MODU"; headerTitle.color = VTheme.Red; }
            if (detailPanel) detailPanel.SetActive(false);
            if (backButton) backButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
            if (closeDetailButton) closeDetailButton.onClick.AddListener(() => detailPanel.SetActive(false));
            if (playButton) playButton.onClick.AddListener(OnPlayChapter);

            PopulateMap();
            ScrollToCurrentChapter();
        }

        void PopulateMap()
        {
            if (StoryManager.Instance == null || nodeContainer == null) return;
            int completed = SaveManager.Instance?.Data.completedChapter ?? 0;

            for (int i = 0; i < StoryManager.Instance.chapters.Length; i++)
            {
                var chapter = StoryManager.Instance.chapters[i];
                int index = i;
                bool isBoss = (i == StoryManager.Instance.chapters.Length - 1) || chapter.difficulty == AIDifficulty.Hard;
                var prefab = isBoss ? bossNodePrefab : normalNodePrefab;
                if (prefab == null) prefab = normalNodePrefab;
                if (prefab == null) continue;

                var node = Instantiate(prefab, nodeContainer);
                var rt = node.GetComponent<RectTransform>();

                // Node number
                var numText = node.GetComponentInChildren<TextMeshProUGUI>();
                if (numText != null)
                    numText.text = isBoss ? "BOSS" : chapter.chapterNumber.ToString();

                // Node color
                var nodeImg = node.GetComponent<Image>();
                if (nodeImg != null)
                {
                    if (i < completed)
                        nodeImg.color = nodeCompleted;
                    else if (i == completed)
                        nodeImg.color = isBoss ? nodeBoss : nodeCurrent;
                    else
                        nodeImg.color = nodeLocked;
                }

                // Stars for completed
                var starsParent = node.transform.Find("Stars");
                if (starsParent != null)
                {
                    starsParent.gameObject.SetActive(i < completed);
                    if (i < completed)
                    {
                        string grade = PlayerPrefs.GetString($"chapter_{i}_grade", "C");
                        int starCount = GradeToStars(grade);
                        for (int s = 0; s < starsParent.childCount; s++)
                        {
                            var starImg = starsParent.GetChild(s).GetComponent<Image>();
                            if (starImg) starImg.color = s < starCount ? starActive : starInactive;
                        }
                    }
                }

                // Click
                bool unlocked = i <= completed;
                var btn = node.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = unlocked;
                    btn.onClick.AddListener(() => ShowDetail(chapter, index));
                }
            }
        }

        void ShowDetail(ChapterData chapter, int index)
        {
            selectedChapter = chapter;
            selectedIndex = index;

            if (detailPanel) detailPanel.SetActive(true);
            if (detailTitle) detailTitle.text = $"Bolum {chapter.chapterNumber}: {chapter.chapterTitle}";
            if (detailDescription) detailDescription.text = chapter.description;
            if (detailDifficulty)
            {
                string diff = chapter.difficulty switch
                {
                    AIDifficulty.Easy => "Kolay",
                    AIDifficulty.Normal => "Normal",
                    AIDifficulty.Hard => "Zor",
                    _ => "Normal"
                };
                detailDifficulty.text = $"Zorluk: {diff}";
            }
            if (detailReward) detailReward.text = $"Odul: {chapter.coinReward} coin";

            if (detailEnemyPortrait && chapter.enemyCharacter != null && chapter.enemyCharacter.portrait != null)
                detailEnemyPortrait.sprite = chapter.enemyCharacter.portrait;

            // Grade stars
            int completed = SaveManager.Instance?.Data.completedChapter ?? 0;
            if (starImages != null && index < completed)
            {
                string grade = PlayerPrefs.GetString($"chapter_{index}_grade", "C");
                int starCount = GradeToStars(grade);
                for (int i = 0; i < starImages.Length; i++)
                    if (starImages[i]) starImages[i].color = i < starCount ? starActive : starInactive;
            }

            if (playButton)
            {
                var playText = playButton.GetComponentInChildren<TextMeshProUGUI>();
                if (playText) playText.text = index < completed ? "TEKRAR OYNA" : "BASLAT";
            }

            UIAudio.Instance?.PlayClick();
        }

        void OnPlayChapter()
        {
            if (selectedChapter == null || StoryManager.Instance == null) return;
            StoryManager.Instance.LoadChapter(selectedIndex);
        }

        void ScrollToCurrentChapter()
        {
            int completed = SaveManager.Instance?.Data.completedChapter ?? 0;
            if (scrollView != null && StoryManager.Instance != null && StoryManager.Instance.chapters.Length > 0)
            {
                float progress = (float)completed / Mathf.Max(1, StoryManager.Instance.chapters.Length - 1);
                scrollView.verticalNormalizedPosition = 1f - progress;
            }
        }

        int GradeToStars(string grade)
        {
            return grade switch { "S" => 3, "A" => 3, "B" => 2, "C" => 1, _ => 0 };
        }
    }
}
