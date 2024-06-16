using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ToggleManager : MonoBehaviour
{
    [SerializeField] private GameObject[] objectsToHide;
    [SerializeField] private GameObject objectToToggle;

    private Image dropdownImg;

    // Start is called before the first frame update
    void Start()
    {
        dropdownImg = GetComponentInChildren<Image>();
    }

    public void UpdateUI(bool isOn)
    {
        if (dropdownImg != null)
        {
            dropdownImg.transform.rotation = isOn ? Quaternion.Euler(0, 0, 180) : Quaternion.Euler(0, 0, 0);
        }
        foreach (GameObject obj in objectsToHide)
        {
            obj.SetActive(false);
        }
        objectToToggle.SetActive(isOn);
    }
}
