using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartupManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
#if UNITY_SERVER
        SceneManager.LoadSceneAsync("00_MainSceneTemp");
#else
        SceneManager.LoadSceneAsync("MainMenu");
#endif
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
