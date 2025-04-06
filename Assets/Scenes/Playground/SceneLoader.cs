using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        SceneManager.LoadSceneAsync("Scene-to-add", LoadSceneMode.Additive);
        StartCoroutine(UnloadScene());
    }

    IEnumerator UnloadScene()
    {
        yield return new WaitForSeconds(5f);
        SceneManager.UnloadSceneAsync("Scene-to-add");
    }
}
