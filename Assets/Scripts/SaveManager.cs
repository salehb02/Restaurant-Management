using UnityEngine;

public class SaveManager : MonoBehaviour
{
    #region Singleton
    public static SaveManager instance;

    private void Awake()
    {
        transform.SetParent(null);

        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeSave();
    }
    #endregion

    // use custom es3settings to setup save cache for load and save speedup
    private ES3Settings _es3Settings;

    private void InitializeSave()
    {
        if (!ES3.FileExists())
            ES3.Save("__InitData__", 1);

        ES3.CacheFile();
        _es3Settings = new ES3Settings(ES3.Location.Cache);
    }

    private void Save() => ES3.StoreCachedFile(_es3Settings);

    public bool HasKey(string key) => ES3.KeyExists(key, _es3Settings);

    public void Set<T>(string key, T value)
    {
        ES3.Save(key, value, _es3Settings);
        Save();
    }

    public T Get<T>(string key)
    {
        if (!HasKey(key))
            return default;

        return ES3.Load<T>(key, _es3Settings);
    }
}
