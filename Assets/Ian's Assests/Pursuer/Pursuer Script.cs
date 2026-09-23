using UnityEngine;

public class PursuerScript : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float speed;

    private void Update()
    {
        if (target != null)
        {
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target.position, step);
        }
    }

    /*
    uses rigidbody movement instead

    private Rigidbody rBody;

    private void Awake()
    {
        rBody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (target != null)
        {
            Vector3 direction = (target.position - rBody.position).normalized;
            rBody.AddForce(direction * speed, ForceMode.Force);
        }
    }
    */
}
