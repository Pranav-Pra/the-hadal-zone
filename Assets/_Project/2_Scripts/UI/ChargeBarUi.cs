using UnityEngine;

// show charge in eahc segment
public class ChargeBarUI : MonoBehaviour
{
    [SerializeField] private ChargeData chargeData;
    [SerializeField] private GameObject[] inners;   // bottom segment first

    void OnEnable()  => chargeData.OnChanged += Refresh;
    void OnDisable() => chargeData.OnChanged -= Refresh;

    void Start() => chargeData.ResetCharge();   // start each game at 0

    void Refresh(int charge)
    {
        for (int i = 0; i < inners.Length; i++)
            inners[inners.Length - 1 - i].SetActive(i >= charge); // so bttomt gets charge first
    }
}