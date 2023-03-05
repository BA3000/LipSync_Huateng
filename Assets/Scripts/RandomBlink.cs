using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomBlink : MonoBehaviour
{
    static float[] Weights = new float[] { 100, 75, 50, 25, 0 };     // 100时眼睛全闭，0时眼睛全开

    public SkinnedMeshRenderer Face;
    bool m_enable = true;

    bool m_blinking;
    float m_timer;
    int m_index;

    public void Update()
    {
        if (!Face || !Enable)
            return;

        m_timer -= Time.deltaTime;

        if (m_blinking)
        {
            if (m_timer < 0)
            {
                m_timer = 0.05f;                    // 每隔0.05s设置一次眨眼动画，可调节眨眼的快慢
                m_index++;

                if (m_index < Weights.Length)
                    SetShape(m_index);              // 设置眨眼动画，从闭到开
                else
                {
                    m_blinking = false;             // 眨眼结束
                    m_timer = Random.Range(3, 5);   // 设置眼睛保持睁开的时间
                }
            }
        }
        else
        {
            if (m_timer < 0)
                ToBlink();                          // 开始眨眼
        }
    }

    void ToBlink()
    {
        m_blinking = true;
        m_timer = 0;
        m_index = -1;
    }

    void SetShape(int index)
    {
        Face.SetBlendShapeWeight(13, Weights[index]);
    }

    public bool Enable
    {
        get { return m_enable; }
        set
        {
            if (m_enable != value)
            {
                m_enable = value;

                if (m_enable)
                    ToBlink();
                else
                    SetShape(Weights.Length - 1);
            }
        }
    }
}
