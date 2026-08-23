using UnityEngine;

namespace VRunner.Core
{
    /// <summary>
    /// Diem load asset duy nhat cho code, hien dung Resources.Load.
    /// Sau nay chuyen sang AssetBundle legacy thi chi sua noi dung 2 ham nay.
    /// </summary>
    public static class AssetProvider
    {
        public static T Load<T>(string path) where T : Object
        {
            T asset = Resources.Load<T>(path);
            if (asset == null)
                Debug.LogError($"AssetProvider: khong tim thay asset tai Resources/{path}");
            return asset;
        }

        public static T[] LoadAll<T>(string folder) where T : Object
        {
            return Resources.LoadAll<T>(folder);
        }
    }
}
