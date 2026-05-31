using UnityEngine;

[DisallowMultipleComponent]
public class ShrimpValue : MonoBehaviour
{
    [SerializeField, Min(1)] private int amount = 1;

    public int Amount => Mathf.Max(1, amount);
}
