using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RespawnCube : MonoBehaviour
{
    /// <summary>
        /// Respawn will happen when the transform moves below this World Y position.
    /// </summary>
    [SerializeField]
    [Tooltip("Respawn will happen when the transform moves below this World Y position.")]
    private float _yThresholdForRespawn;

    /// <summary>
        /// UnityEvent triggered when a respawn occurs.
    /// </summary>
    [SerializeField]
    [Tooltip("UnityEvent triggered when a respawn occurs.")]
    private UnityEvent _whenRespawned = new UnityEvent();

      /// <summary>
        /// If the transform has an associated rigidbody, make it kinematic during this
        /// number of frames after a respawn, in order to avoid ghost collisions.
    /// </summary>
    [SerializeField]
    [Tooltip("If the transform has an associated rigidbody, make it kinematic during this number of frames after a respawn, in order to avoid ghost collisions.")]
    private int _sleepFrames = 0;
    public UnityEvent WhenRespawned => _whenRespawned;

    // cached starting transform
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Vector3 _initialScale;

    private Rigidbody _rigidBody;
    private int _sleepCountDown;

   protected virtual void OnEnable()
    {
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
        _initialScale = transform.localScale;
        _rigidBody = GetComponent<Rigidbody>();
    }
    protected virtual void Update()
    {
        if (transform.position.y < _yThresholdForRespawn){
            Invoke("Respawn",2f);
        }
    }

    protected virtual void FixedUpdate()
        {
            if (_sleepCountDown > 0)
            {
                if (--_sleepCountDown == 0)
                {
                    _rigidBody.isKinematic = false;
                }
            }
        }

    public void Respawn()
        {
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            transform.localScale = _initialScale;

            if (_rigidBody)
            {
                _rigidBody.velocity = Vector3.zero;
                _rigidBody.angularVelocity = Vector3.zero;

                if (!_rigidBody.isKinematic && _sleepFrames > 0)
                {
                    _sleepCountDown = _sleepFrames;
                    _rigidBody.isKinematic = true;
                }
            }

            

            _whenRespawned.Invoke();
        }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    
}
