using TMPro;
using UnityEngine;
using UnityEngine.UI;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AdvancedGears
{
    public class LicenseSceneManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI licenseText;
        [SerializeField] Button backButton;

        private const string LicenseResourcePath = "Licenses/ThirdPartyLicenses";
        private const string BackSceneName = "MainScene";

        private void Start()
        {
            var asset = Resources.Load<TextAsset>(LicenseResourcePath);
            if (asset != null)
                licenseText.text = asset.text;
            else
                licenseText.text = "ライセンス情報を読み込めませんでした。";

            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        private void OnBackButtonClicked()
        {
            USceneManager.LoadScene(BackSceneName);
        }
    }
}
