using UnityEngine;

[System.Serializable]
public class CustomerSeat
{
    public Transform seatTransform;
    public bool isOccupied = false;
    public CustomerWithMovement occupyingCustomer = null; // Updated type

    [Header("Seat Properties")]
    public SeatType seatType = SeatType.Regular;
    public float comfortBonus = 0f; // Memnuniyet bonusu

    public Vector3 GetSeatPosition()
    {
        return seatTransform != null ? seatTransform.position : Vector3.zero;
    }

    public void OccupySeat(CustomerWithMovement customer) // Updated parameter type
    {
        isOccupied = true;
        occupyingCustomer = customer;
    }

    public void FreeSeat()
    {
        isOccupied = false;
        occupyingCustomer = null;
    }
}

public enum SeatType
{
    Regular,
    Premium,
    Window,
    Corner
}
