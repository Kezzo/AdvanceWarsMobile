﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FpsCounter : MonoBehaviour
{
    [SerializeField]
    private Text m_fpsCounterText;

    private int m_framesPerSecond;

    void Start()
    {
        StartCoroutine(DisplayFps());
    }

    private IEnumerator DisplayFps()
    {
        while (true)
        {
            m_framesPerSecond = (int)(1.0f / Time.smoothDeltaTime);
            m_fpsCounterText.text = m_framesPerSecond + " FPS";

            m_fpsCounterText.color = m_framesPerSecond < 20 ? Color.red : Color.white;

            yield return new WaitForSeconds(0.5f);
        }
    }
}
