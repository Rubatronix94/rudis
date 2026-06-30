using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemigo melee estilo Dark Souls con root motion.
/// Requiere: Animator con parámetros float "Horizontal", "Vertical" y Trigger "Atacar"
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMeleeController : MonoBehaviour
{
    // ─── Referencia al jugador ───────────────────────────────────────────
    [Header("Referencias")]
    [Tooltip("Transform del jugador. Si se deja vacío se busca por tag 'Player'.")]
    public Transform player;

    // ─── Rangos de comportamiento ────────────────────────────────────────
    [Header("Rangos")]
    [Tooltip("Distancia máxima para que el enemigo comience a perseguir al jugador.")]
    public float rangoDeteccion = 12f;

    [Tooltip("Distancia a la que el enemigo se detiene y puede atacar.")]
    public float rangoAtaque = 1.8f;

    [Tooltip("Distancia mínima que el enemigo trata de mantener respecto al jugador.")]
    public float rangoMinimoStop = 1.4f;

    // ─── Comportamiento de strafe ────────────────────────────────────────
    [Header("Strafe / Circulación")]
    [Tooltip("Velocidad angular al rodear al jugador (grados/seg, puede ser negativa para invertir sentido).")]
    public float velocidadStrafe = 60f;

    [Tooltip("Tiempo mínimo en segundos antes de cambiar de dirección de strafe.")]
    public float tiempoMinimoStrafe = 1.5f;

    [Tooltip("Tiempo máximo en segundos antes de cambiar de dirección de strafe.")]
    public float tiempoMaximoStrafe = 3.5f;

    [Tooltip("Probabilidad de que el enemigo haga strafe en vez de avanzar recto (0-1).")]
    [Range(0f, 1f)]
    public float probabilidadStrafe = 0.6f;

    // ─── Ataque ──────────────────────────────────────────────────────────
    [Header("Ataque")]
    [Tooltip("Tiempo de espera entre ataques.")]
    public float cooldownAtaque = 1.8f;

    // ─── Suavizado del Animator ──────────────────────────────────────────
    [Header("Animator")]
    [Tooltip("Velocidad de interpolación para los parámetros del Animator.")]
    public float suavizadoAnimator = 8f;

    // ─── Debug visual ────────────────────────────────────────────────────
    [Header("Debug")]
    public bool mostrarGizmos = true;

    // ─── Privadas ────────────────────────────────────────────────────────
    Animator _anim;
    NavMeshAgent _agent;

    // Estado de strafe
    float _dirStrafe = 1f;   // 1 = derecha, -1 = izquierda
    float _timerStrafe = 0f;
    float _duracionStrafe = 0f;
    bool _haciendo_strafe = false;

    // Cooldown ataque
    float _timerAtaque = 0f;

    // Parámetros Animator suavizados
    float _animH = 0f;
    float _animV = 0f;

    // IDs cacheados para eficiencia
    static readonly int ID_Horizontal = Animator.StringToHash("Horizontal");
    static readonly int ID_Vertical = Animator.StringToHash("Speed");
    static readonly int ID_Atacar = Animator.StringToHash("Atacar");

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _anim = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();

        // Root motion controlado por nosotros
        _agent.updatePosition = false;
        _agent.updateRotation = false;

        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }

        ReiniciarTimerStrafe();
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (player == null) return;

        float distancia = Vector3.Distance(transform.position, player.position);

        ActualizarTimers();

        if (distancia <= rangoDeteccion)
        {
            GirarHaciaJugador();

            if (distancia <= rangoAtaque)
                EstadoAtaque(distancia);
            else
                EstadoMovimiento(distancia);
        }
        else
        {
            // Fuera de rango: parar
            SetAnimatorBlend(0f, 0f);
        }
    }

    // ─── Girar suavemente hacia el jugador ───────────────────────────────
    void GirarHaciaJugador()
    {
        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion objetivo = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, objetivo, Time.deltaTime * 10f);
    }

    // ─── Estado: movimiento / strafe ─────────────────────────────────────
    void EstadoMovimiento(float distancia)
    {
        _timerAtaque = Mathf.Max(_timerAtaque, 0f); // sigue contando pero no ataca

        if (_haciendo_strafe)
        {
            // Rodear al jugador en arco
            float radio = distancia;
            float angulo = _dirStrafe * velocidadStrafe * Time.deltaTime;
            Vector3 offset = Quaternion.Euler(0f, angulo, 0f) * (transform.position - player.position);
            Vector3 destino = player.position + offset.normalized * radio;

            MoverAgente(destino);
            SetAnimatorBlend(_dirStrafe * 0.7f, 0f); // strafe puro lateral
        }
        else
        {
            // Avanzar directo
            Vector3 destino = player.position;
            MoverAgente(destino);
            SetAnimatorBlend(0f, 1f);
        }
    }

    // ─── Estado: dentro de rango de ataque ───────────────────────────────
    void EstadoAtaque(float distancia)
    {
        // Si está demasiado cerca, dar un paso atrás
        if (distancia < rangoMinimoStop)
        {
            SetAnimatorBlend(0f, -0.5f);
            return;
        }

        // Idle de combate (pequeño strafe en sitio)
        SetAnimatorBlend(_dirStrafe * 0.3f, 0f);

        if (_timerAtaque <= 0f)
        {
            _anim.SetTrigger(ID_Atacar);
            _timerAtaque = cooldownAtaque;
        }
    }

    // ─── Mover el NavMeshAgent ────────────────────────────────────────────
    void MoverAgente(Vector3 destino)
    {
        if (NavMesh.SamplePosition(destino, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }

    // ─── Timers ───────────────────────────────────────────────────────────
    void ActualizarTimers()
    {
        // Cooldown ataque
        if (_timerAtaque > 0f)
            _timerAtaque -= Time.deltaTime;

        // Timer de strafe
        _timerStrafe -= Time.deltaTime;
        if (_timerStrafe <= 0f)
        {
            ReiniciarTimerStrafe();
        }
    }

    void ReiniciarTimerStrafe()
    {
        _duracionStrafe = Random.Range(tiempoMinimoStrafe, tiempoMaximoStrafe);
        _timerStrafe = _duracionStrafe;
        _haciendo_strafe = Random.value < probabilidadStrafe;
        if (_haciendo_strafe)
            _dirStrafe = Random.value < 0.5f ? 1f : -1f;
    }

    // ─── Suavizar parámetros del Animator ────────────────────────────────
    void SetAnimatorBlend(float h, float v)
    {
        _animH = Mathf.Lerp(_animH, h, Time.deltaTime * suavizadoAnimator);
        _animV = Mathf.Lerp(_animV, v, Time.deltaTime * suavizadoAnimator);
        _anim.SetFloat(ID_Horizontal, _animH);
        _anim.SetFloat(ID_Vertical, _animV);
    }

    // ─── Root Motion: aplicar movimiento del Animator al NavMeshAgent ─────
    void OnAnimatorMove()
    {
        // Aplicamos la posición que calcula el root motion de la animación
        Vector3 rootPos = _anim.rootPosition;

        // Si el agente tiene destino, mezclamos root motion con navegación
        if (_agent.remainingDistance > _agent.stoppingDistance && !_agent.pathPending)
        {
            // Avance: root motion dirige, el agente corrige lateralmente
            _agent.nextPosition = rootPos;
            transform.position = rootPos;
        }
        else
        {
            // En combate o idle: root motion puro (ataques, pasos de strafe)
            transform.position = rootPos;
            _agent.nextPosition = rootPos;
        }
    }

    // ─── Gizmos de debug ─────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (!mostrarGizmos) return;

        // Rango detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        // Rango ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);

        // Rango mínimo
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, rangoMinimoStop);
    }
}