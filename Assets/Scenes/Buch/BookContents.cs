using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convai.Scripts.Runtime.Features;

namespace ChristinaCreatesGames.Typography.Book
{
    public class BookContents : MonoBehaviour
    {
        [TextArea(10, 20)]
        [SerializeField] private string content;
        [Space]
        [SerializeField] private TMP_Text leftSide;
        [SerializeField] private TMP_Text rightSide;
        [Space]
        [SerializeField] private TMP_Text leftPagination;
        [SerializeField] private TMP_Text rightPagination;

        [Header("UI Controls")]
        [Tooltip("Das GameObject, das den Next-Page-Knopf enthält")]
        [SerializeField] private GameObject nextPageObject;

        [Header("Narrative Design Trigger")]
        [SerializeField] private NarrativeDesignTrigger narrativeTrigger;

        [Tooltip("Liste der Seitenzahlen und zugehörigen ConvAI-Triggernamen")]
        [SerializeField] private List<PageTrigger> pageTriggers = new();
        private Dictionary<int, string> _triggerDictionary;

        [System.Serializable]
        public class PageTrigger
        {
            public int pageNumber;
            public string eventName;
        }

        [Header("Choice → neuer Text")]
        [Tooltip("Mapping von Section-ID zu neuem Buchtext")]
        [SerializeField] private List<ChoiceContent> choiceContents = new();
        private Dictionary<string, string> _choiceMap;

        [System.Serializable]
        public class ChoiceContent
        {
            public string choiceName;
            [TextArea(5, 10)]
            public string newContent;
        }

        private void Awake()
        {
            Debug.Log("[BookContents] Awake: Setup content and build dictionaries.");
            SetupContent();
            UpdatePagination();
            BuildTriggerDictionary();
            BuildChoiceMap();
        }

        private void Start()
        {
            Debug.Log("[BookContents] Start: Initializing section subscriptions.");
            StartCoroutine(InitSectionSubscriptions());
        }

        private IEnumerator InitSectionSubscriptions()
        {
            yield return new WaitUntil(() =>
                narrativeTrigger != null &&
                narrativeTrigger.convaiNPC != null &&
                narrativeTrigger.convaiNPC.narrativeDesignManager != null &&
                narrativeTrigger.convaiNPC.narrativeDesignManager.sectionChangeEventsDataList != null &&
                narrativeTrigger.convaiNPC.narrativeDesignManager.sectionChangeEventsDataList.Count > 0
            );

            SubscribeSectionEvents();
        }

        private void BuildTriggerDictionary()
        {
            _triggerDictionary = new Dictionary<int, string>();
            foreach (var pt in pageTriggers)
            {
                if (!_triggerDictionary.ContainsKey(pt.pageNumber))
                {
                    _triggerDictionary.Add(pt.pageNumber, pt.eventName);
                    Debug.Log($"[BookContents] Trigger-Dictionary: Seite {pt.pageNumber} → Event '{pt.eventName}'.");
                }
            }
        }

        private void BuildChoiceMap()
        {
            _choiceMap = new Dictionary<string, string>();
            foreach (var cc in choiceContents)
            {
                if (!_choiceMap.ContainsKey(cc.choiceName))
                {
                    _choiceMap.Add(cc.choiceName, cc.newContent);
                    Debug.Log($"[BookContents] Choice-Map: Section '{cc.choiceName}' → neuer Text geladen.");
                }
            }
        }

        private void SetupContent()
        {
            leftSide.text = content;
            rightSide.text = content;
            Debug.Log($"[BookContents] SetupContent: Text gesetzt (Länge: {content.Length} Zeichen)");
        }

        private void UpdatePagination()
        {
            leftPagination.text = leftSide.pageToDisplay.ToString();
            rightPagination.text = rightSide.pageToDisplay.ToString();
            Debug.Log($"[BookContents] UpdatePagination: links={leftSide.pageToDisplay}, rechts={rightSide.pageToDisplay}");
        }

        public void NextPage()
        {
            int currentLeft = leftSide.pageToDisplay;
            int currentRight = rightSide.pageToDisplay;
            int total = rightSide.textInfo.pageCount;
            Debug.Log($"[BookContents] NextPage clicked: left={currentLeft}, right={currentRight}, totalPages={total}");

            if (currentRight >= total)
            {
                Debug.Log($"[BookContents] Ende erreicht auf Seite {currentRight} von {total}");
                if (_triggerDictionary != null && _triggerDictionary.TryGetValue(currentLeft, out string eventName))
                {
                    narrativeTrigger.UpdateAvailableTriggers();
                    int idx = narrativeTrigger.availableTriggers.IndexOf(eventName);
                    Debug.Log($"[BookContents] availableTriggers enthält {narrativeTrigger.availableTriggers.Count} Einträge");

                    if (idx >= 0)
                    {
                        narrativeTrigger.selectedTriggerIndex = idx;
                        narrativeTrigger.InvokeSelectedTrigger();
                        Debug.Log($"[BookContents] Trigger '{eventName}' (Index {idx}) ausgelöst.");
                    }
                    else
                        Debug.LogWarning($"[BookContents] Trigger '{eventName}' nicht in availableTriggers gefunden.");

                    if (nextPageObject != null)
                    {
                        nextPageObject.SetActive(false);
                        Debug.Log("[BookContents] NextPage-Button deaktiviert.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[BookContents] Kein Trigger für Seite {currentLeft} definiert.");
                }
                return;
            }

            Debug.Log("[BookContents] Normales Blättern");
            if (leftSide.pageToDisplay >= total - 1)
                leftSide.pageToDisplay = total - 1;
            else
                leftSide.pageToDisplay += 2;

            rightSide.pageToDisplay = leftSide.pageToDisplay + 1;
            UpdatePagination();
            CheckForTrigger();
        }

        private void CheckForTrigger()
        {
            int currentPage = leftSide.pageToDisplay;
            Debug.Log($"[BookContents] CheckForTrigger auf Seite {currentPage}");
            if (_triggerDictionary != null && _triggerDictionary.TryGetValue(currentPage, out string eventName))
            {
                Debug.Log($"[BookContents] Trigger gefunden: {eventName}");
                narrativeTrigger.UpdateAvailableTriggers();
                int idx = narrativeTrigger.availableTriggers.IndexOf(eventName);
                if (idx >= 0)
                {
                    narrativeTrigger.selectedTriggerIndex = idx;
                    narrativeTrigger.InvokeSelectedTrigger();
                    Debug.Log($"[BookContents] Trigger '{eventName}' ausgelöst.");
                    if (nextPageObject != null)
                    {
                        nextPageObject.SetActive(false);
                        Debug.Log("[BookContents] NextPage-Button deaktiviert nach Trigger.");
                    }
                }
                else
                    Debug.LogWarning($"[BookContents] Trigger '{eventName}' nicht gefunden.");
            }
            else
                Debug.Log($"[BookContents] Kein Trigger für Seite {currentPage}.");
        }

        private void SubscribeSectionEvents()
        {
            var ndManager = narrativeTrigger.convaiNPC.narrativeDesignManager;
            Debug.Log($"[BookContents] Registriere {ndManager.sectionChangeEventsDataList.Count} Section-Events");

            foreach (var sc in ndManager.sectionChangeEventsDataList)
            {
                string sectionId = sc.id;
                sc.onSectionStart.AddListener(() => HandleSectionStart(sectionId));
                Debug.Log($"[BookContents] Listener für Section '{sectionId}' registriert.");
            }
        }

        private void HandleSectionStart(string sectionId)
        {
            Debug.Log($"[BookContents] HandleSectionStart: Abschnitt '{sectionId}' gestartet.");
            if (_choiceMap.TryGetValue(sectionId, out string nextText))
            {
                Debug.Log($"[BookContents] Alter Content-Länge: {content.Length}");
                // Text anhängen, nicht ersetzen
                if (content == ""){
                    content = nextText;
                }
                else{
                    content += "/n" + "/n" +  nextText;
                }
                
                Debug.Log($"[BookContents] Neuer Content-Länge: {content.Length}");

                SetupContent();
                UpdatePagination();
                Debug.Log("[BookContents] SetupContent und UpdatePagination nach Text-Anhang ausgeführt.");

                if (nextPageObject != null)
                {
                    nextPageObject.SetActive(true);
                    Debug.Log("[BookContents] NextPage-Button wieder aktiviert.");
                }
            }
            else
            {
                Debug.LogWarning($"[BookContents] Kein Text-Mapping für Section '{sectionId}' gefunden.");
            }
        }
    }
}
