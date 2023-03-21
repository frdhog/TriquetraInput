using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;

namespace Triquetra.Input
{
    [Serializable]
    public class KeyboardKey
    {
        [XmlAttribute] public KeyCode PrimaryKey = KeyCode.None;
        [XmlAttribute] public KeyCode SecondaryKey = KeyCode.None;

        [XmlIgnore] public bool PrimaryKeyDown = false;
        [XmlIgnore] public bool SecondaryKeyDown = false;
        [XmlIgnore] public float PrimaryPressTime;
        [XmlIgnore] public float SecondaryPressTime;

        [XmlAttribute] public bool IsAxis = false;
        [XmlAttribute] public bool IsRepeatButton = false;

        [XmlAttribute] public float Smoothing = 0.5f;

        public int GetAxisTranslatedValue()
        {
            if (UnityEngine.Input.GetKeyDown(PrimaryKey))
                PrimaryPressTime = Time.time;

            if (UnityEngine.Input.GetKeyDown(SecondaryKey))
                SecondaryPressTime = Time.time;

            bool isPrimaryPressed = UnityEngine.Input.GetKey(PrimaryKey);
            bool isSecondaryPressed = UnityEngine.Input.GetKey(SecondaryKey);

            int translatedValue = Binding.AxisMiddle;
            if (isPrimaryPressed && !isSecondaryPressed)
                translatedValue = (int)Mathf.Lerp(Binding.AxisMiddle, Binding.AxisMax, (Time.time - PrimaryPressTime) / Smoothing);
            else if (isSecondaryPressed && !isPrimaryPressed)
                translatedValue = (int)Mathf.Lerp(Binding.AxisMiddle, Binding.AxisMin, (Time.time - SecondaryPressTime) / Smoothing);

            return translatedValue;
        }
    }
}
