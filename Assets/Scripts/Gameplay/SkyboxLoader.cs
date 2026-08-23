using UnityEngine;
using VRunner.Core;

namespace VRunner.Gameplay
{
    /// <summary>
    /// Gan RenderSettings.skybox bang code thay vi tham chieu tinh trong Lighting Settings cua scene.
    /// Bat buoc phai vay: material/texture skybox nam trong bundle "texture_skybox" (remote, tai tu CDN) -
    /// neu con tham chieu tinh tu scene, Unity se nhung ban sao vao build chinh bat ke bundle name,
    /// lam mat tac dung tach CDN (xem docs/asset-bundle.md).
    /// </summary>
    public class SkyboxLoader : MonoBehaviour
    {
        [SerializeField] private string skyboxResourcePath = "Texture/Skybox/mat_skybox-day";

        private void Awake()
        {
            Material skybox = AssetProvider.Load<Material>(skyboxResourcePath);
            if (skybox != null)
                RenderSettings.skybox = skybox;
        }
    }
}
