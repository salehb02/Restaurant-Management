using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Singleton : MonoBehaviour
{
    [SerializeField] GameObject m_persistantObject;
    bool hasInstantiate = false;
    private void Awake()
    {
        if (hasInstantiate) return;

        SpawnObject();

        hasInstantiate = true;
    }


    void SpawnObject()
    {
        GameObject persistentObject = Instantiate(m_persistantObject);
        DontDestroyOnLoad(persistentObject);
    }




}
