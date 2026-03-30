using UnityEngine;

public class WheelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WheelCollider wheelCollider;
    [SerializeField] private GameObject wheelObj;
    [SerializeField] private GameObject innerWheelObj;
    [SerializeField] private AudioSource screechNoise;
    [SerializeField] private GameObject particles;
    [SerializeField] private GameObject wheelCoverObj;

    [Header("Settings")]
    private const string SLIPPING_EVENT_NAME = "s";

    private bool isOwned = false;
    private Vector3 lastPos = Vector3.zero;
    private bool isSlipping = false;
    private bool slippingEventCheck = false;

    // EXPOSED FOR CAR DRIVER / FFB
    public float currentSlip { get; private set; } = 0f;

    [Header("Traction Settings")]
    [Tooltip("Stiffness multiplier for forward grip. Default is 1. Increase for more acceleration/braking traction.")]
    [SerializeField] private float forwardGrip = 1.5f;
    [Tooltip("Stiffness multiplier for sideways grip. Default is 1. Increase to reduce drifting/sliding in turns.")]
    [SerializeField] private float sidewaysGrip = 1.5f;

    private void ApplyGripSettings()
    {
        WheelFrictionCurve fFriction = wheelCollider.forwardFriction;
        fFriction.stiffness = forwardGrip;
        wheelCollider.forwardFriction = fFriction;

        WheelFrictionCurve sFriction = wheelCollider.sidewaysFriction;
        sFriction.stiffness = sidewaysGrip;
        wheelCollider.sidewaysFriction = sFriction;
    }

    private void Start()
    {
        lastPos = transform.position;
        ApplyGripSettings();
    }

    private void Update()
    {
        wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        wheelObj.transform.position = pos;
        wheelObj.transform.rotation = rot * Quaternion.Euler(0, -90, 0);

        float speed = (2 * Mathf.PI * wheelCollider.radius) * (wheelCollider.rpm / 60);

        if (wheelCoverObj != null)
        {
            wheelCoverObj.transform.position = pos;
            wheelCoverObj.transform.rotation = wheelCollider.transform.rotation * Quaternion.Euler(wheelCollider.steerAngle, wheelCollider.steerAngle, 0);
        }

        if (!isOwned)
        {
            float movementDist = Vector3.Distance(transform.position, lastPos);
            speed = movementDist / Time.deltaTime;

            float dot = Vector3.Dot(transform.parent.right, lastPos - transform.position);
            float sign = 1;
            if (dot < 0) sign = -1;
            else if (dot == 0) sign = 0;

            float wheelTurnsDeg = (360 * movementDist / (2 * wheelCollider.radius * Mathf.PI)) * sign;
            innerWheelObj.transform.Rotate(new Vector3(0, 0, wheelTurnsDeg), Space.Self);
            lastPos = wheelCollider.transform.position;
        }

        screechNoise.volume = Mathf.Lerp(0, 1, Mathf.InverseLerp(0, 20, speed));

        if (isSlipping)
        {
            if (!screechNoise.isPlaying)
            {
                screechNoise.Play();
                if (particles != null) particles.SetActive(true);
            }
        }
        else
        {
            if (screechNoise.isPlaying)
            {
                screechNoise.Stop();
                if (particles != null) particles.SetActive(false);
            }
        }
    }

    private void FixedUpdate()
    {
        if (isOwned)
        {
            WheelHit wheelHit;
            bool isWheelHittingGround = wheelCollider.GetGroundHit(out wheelHit);

            if (isWheelHittingGround)
            {
                // Store the raw magnitude for FFB Rumble
                currentSlip = Mathf.Max(Mathf.Abs(wheelHit.forwardSlip), Mathf.Abs(wheelHit.sidewaysSlip));
                isSlipping = currentSlip > 1 || Mathf.Abs(wheelHit.sidewaysSlip) > 0.2f;
            }
            else
            {
                currentSlip = 0f;
                isSlipping = false;
            }

            if (isSlipping && !slippingEventCheck)
            {
                slippingEventCheck = true;
            }
            else if (!isSlipping && slippingEventCheck)
            {
                slippingEventCheck = false;
            }
        }
    }

    private void OnBecomeOwner() { isOwned = true; }
    private void OnLostOwnership() { isOwned = false; }
    private void HandleSlippingEvent(bool slip) { isSlipping = slip; }
}