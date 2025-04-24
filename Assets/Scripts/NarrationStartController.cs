using Convai.Scripts.Runtime.Features;
using UnityEngine;

public class NarrationStartController : MonoBehaviour
{    
    [SerializeField]
    private NarrativeDesignTrigger narrativeDesignTrigger;

     [SerializeField]
    private bool active;

    private bool narrationStarted;

    // Start is called before the first frame update
    void Update()
    {   
        if (active)
        {
            StartNarration ();
            
        }        
    }

    void StartNarration ()
    {
        if(!narrationStarted)
        {
            narrativeDesignTrigger.InvokeSelectedTrigger();
            narrationStarted = true;
        }
    }
}
