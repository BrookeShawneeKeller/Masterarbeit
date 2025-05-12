using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convai.Scripts.Runtime.Features;

namespace ChristinaCreatesGames.Typography.Book
{
    public class BookContents : MonoBehaviour
    {
        //VARIABLEN / PARAMETER -> werden im Editor gesetzt.
        [TextArea(10, 20)]
        [SerializeField] private string content;
        [Space]


        [Header("Page References)")]
        [Tooltip("Left and right page of book")]
        [SerializeField] private TMP_Text leftSide;
        [SerializeField] private TMP_Text rightSide;
        [Space]


        [Header("Page Navigation (Pagination)")]
        [Tooltip("Left and right page number text field")]
        [SerializeField] private TMP_Text leftPagination;
        [SerializeField] private TMP_Text rightPagination;


        [Header("UI Controls")]
        [Tooltip("Next-Page Button Reference")]
        [SerializeField] private GameObject nextPageObject;
        [SerializeField] private GameObject talkTaste;


        [Header("Narrative Design Trigger Script (ConvAI)")]
        [SerializeField] private NarrativeDesignTrigger narrativeTrigger;


        [Header("3d-Book Animator")]
        [SerializeField] private Animator prefabAnimator;   


        [Tooltip("Liste der Seitenzahlen und zugehörigen ConvAI-Triggernamen")]
        [SerializeField] private List<PageTrigger> pageTriggers = new();
        private Dictionary<int, string> _triggerDictionary;


        [System.Serializable]
        public class PageTrigger
        {
            public int pageNumber; //Die Numer welche das Event triggered
            public string eventName; //Das Event, welches bei der Nummer getriggered wird. EventName von ConvAI Narratvie Design
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
            public string newContent; //Text welcher zur Choice gehört

        }


        // Neue Variable, um den Start-Trigger einmalig auszuführen
        [SerializeField] private bool startTriggerExecuted = false;




        //FUNKTION

        //Funktion Awake wird einmal zu Beginn ausgeführt.
        //Erstellt die Dictionaires anhand von Input und updated das Buch.    
        private void Awake()
        {
            Debug.Log("[BookContents] Awake: Setup content and build dictionaries.");
            SetupContent();
            UpdatePagination();
            BuildTriggerDictionary();
            BuildChoiceMap();
        }

    
        //Start die Section Subscriptions -> Events werden gemappt zu den 'Listener'
        private void Start()
        {
            Debug.Log("[BookContents] Start: Initializing section subscriptions.");
            StartCoroutine(InitSectionSubscriptions());

            // Optionaler NPC-Starttrigger (einmalig bei Spielstart)
            if (!startTriggerExecuted)
            {
                if (_triggerDictionary.TryGetValue(0, out string startEvent)) // Seite 0 als Starttrigger
                {
                    narrativeTrigger.UpdateAvailableTriggers();
                    int idx = narrativeTrigger.availableTriggers.IndexOf(startEvent);
                    if (idx >= 0)
                    {
                        narrativeTrigger.selectedTriggerIndex = idx;
                        narrativeTrigger.InvokeSelectedTrigger();
                        Debug.Log($"[BookContents] Starttrigger '{startEvent}' ausgelöst.");
                    }
                    startTriggerExecuted = true; // Start-Trigger wurde ausgeführt
                }
            }
        }


        //Section Events von ConvAI Character, welche wir 'beobachten' müssen, um darauf zu reagieren können.
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


        //Erstellt das Trigger Dictionary -> bestimmte Seitenzahlen triggern ein Event, hier hinterlegt.
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


        //Die ChoiceMap beinhaltet welche Section ID welchen Text ans Buch hängt. Somit ist der Ablauf des Buches abhängig von Spieler Input.
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


        //Textseiten werden mit Content gefüllt -> dies ist der Buchtext
        private void SetupContent()
        {
            leftSide.text = content;
            rightSide.text = content;
            Debug.Log($"[BookContents] SetupContent: Text gesetzt (Länge: {content.Length} Zeichen)");
        }


        //Seitenzahlen am unteren Rand des Buches werden upgedated
        private void UpdatePagination()
        {
            leftPagination.text = leftSide.pageToDisplay.ToString();
            rightPagination.text = rightSide.pageToDisplay.ToString();
            Debug.Log($"[BookContents] UpdatePagination: links={leftSide.pageToDisplay}, rechts={rightSide.pageToDisplay}");
        }


        //Diese Funktion bestimmt was passiert, beim Klick auf den "Next Page Button".
        public void NextPage()
        {
            int currentLeft = leftSide.pageToDisplay;
            int currentRight = rightSide.pageToDisplay;
            int total = rightSide.textInfo.pageCount;
            Debug.Log($"[BookContents] NextPage clicked: left={currentLeft}, right={currentRight}, totalPages={total}");
            
            if (currentRight >= total)
            {
                Debug.Log($"[BookContents] Ende erreicht auf Seite {currentRight} von {total}");
                
                // CheckForTrigger wird hier einmalig aufgerufen, wenn das Ende erreicht ist
                CheckForTrigger();

                if (nextPageObject != null)
                {
                    nextPageObject.SetActive(false);
                    Debug.Log("[BookContents] NextPage-Button deaktiviert.");
                }

                if (talkTaste != null)
                {
                    talkTaste.SetActive(true);
                    Debug.Log("[BookContents] Talk Taste aktiviert.");
                }
                return;
            }
            //Wenn wir nicht das Ende des Buches erreicht haben, dann spielen wir die Seiten-Blättern Animation ab.
            else
                prefabAnimator.SetTrigger("AnimatePage");

            Debug.Log("[BookContents] Normales Blättern");
            if (leftSide.pageToDisplay >= total - 1)
                leftSide.pageToDisplay = total - 1;
            else
                leftSide.pageToDisplay += 2;

            rightSide.pageToDisplay = leftSide.pageToDisplay + 1;
            UpdatePagination();
        }


        //Wir prüfen, ob ein Trigger korrekt gesetzt wurde, am Ende des Buchtexts.
        //Wenn ja, dann triggern wir ein Event, mit einer Frage des ConvAI Characters.
        //Nach Beantwortung durch Spieler -> neuer Buchtext wird hinzugefügt.
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

                    if (talkTaste != null)
                    {
                    talkTaste.SetActive(false);
                    Debug.Log("[BookContents] Talk Taste deaktiviert nach Trigger.");
                    }
                }
                else
                    Debug.LogWarning($"[BookContents] Trigger '{eventName}' nicht gefunden.");
            }
            else
                Debug.Log($"[BookContents] Kein Trigger für Seite {currentPage}.");
        }

    
        //Wir fügen 'Listener' pro Section Event hinzu. Diese werden gebraucht, wenn der Character eine Frage stellt und der Spieler eine Antwort geben muss.
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


        //Prüft den Start einer neuen Section und passt den Text an.
        //Aktiviert auch den Button um blättern zu können.
        private void HandleSectionStart(string sectionId)
        {
            Debug.Log($"[BookContents] HandleSectionStart: Abschnitt '{sectionId}' gestartet.");
            if (_choiceMap.TryGetValue(sectionId, out string nextText))
            {
                Debug.Log($"[BookContents] Alter Content-Länge: {content.Length}");
                // Text anhängen, nicht ersetzen
                if (content == "")
                {
                    content = nextText;
                }
                else
                {
                    content += nextText;
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
                if (talkTaste != null)
                    {
                    talkTaste.SetActive(false);
                    Debug.Log("[BookContents] Talk Taste deaktiviert..");
                    }
            }
            else
            {
                Debug.LogWarning($"[BookContents] Kein Text-Mapping für Section '{sectionId}' gefunden.");
            }
        }
    }
}
