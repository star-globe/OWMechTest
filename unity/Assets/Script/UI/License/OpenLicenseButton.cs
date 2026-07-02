using UnityEngine;
using UnityEngine.UI;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AdvancedGears
{
    [RequireComponent(typeof(Button))]
    public class OpenLicenseButton : MonoBehaviour
    {
        private const string LicenseSceneName = "License";

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() => USceneManager.LoadScene(LicenseSceneName));
        }
    }
}
