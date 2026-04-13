using System;
using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Shake Settings")]
    [SerializeField] private float defaultIntensity = 1f;
    [SerializeField] private float defaultDuration = 0.5f;
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private float shakeFrequency = 25f; // Sallanma frekansı

    private Camera cam;
    private PlayerCamera playerCamera; // PlayerCamera referansı
    private Vector3 shakeOffset = Vector3.zero; // Sadece sallanma offset'i
    private Coroutine shakeCoroutine;
    private bool isShaking = false;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
          //  DontDestroyOnLoad(gameObject);
        }
        else
        {
          //  Destroy(gameObject);
            return;
        }

        InitializeCamera();
    }

    void LateUpdate()
    {
        // PlayerCamera hareket ettikten sonra shake offset'ini uygula
        if (isShaking && cam != null)
        {
            cam.transform.position += shakeOffset;
        }
    }

    void Start()
    {
        // PlayerCamera bileşenini bul
        playerCamera = GetComponent<PlayerCamera>();
        if (playerCamera == null)
        {
            Debug.LogWarning("CameraShake: PlayerCamera component not found. Shake will work but may not integrate perfectly with camera following.");
        }

        // Shake offset'ini sıfırla
        shakeOffset = Vector3.zero;
    }

    private void InitializeCamera()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("CameraShake: No camera found! Please attach this script to a camera or ensure Camera.main exists.");
            }
        }
    }

    /// <summary>
    /// Kamerayı varsayılan ayarlarla sallar
    /// </summary>
    public void Shake()
    {
        Shake(defaultIntensity, defaultDuration);
    }

    /// <summary>
    /// Kamerayı belirtilen şiddet ile sallar
    /// </summary>
    /// <param name="intensity">Sallanma şiddeti (0-10 arası önerilir)</param>
    public void Shake(float intensity)
    {
        Shake(intensity, defaultDuration);
    }

    /// <summary>
    /// Kamerayı belirtilen şiddet ve süre ile sallar
    /// </summary>
    /// <param name="intensity">Sallanma şiddeti</param>
    /// <param name="duration">Sallanma süresi</param>
    public void Shake(float intensity, float duration)
    {
        if (cam == null)
        {
            Debug.LogWarning("CameraShake: Camera is null, cannot shake!");
            return;
        }

        // Eğer zaten sallanıyorsa, mevcut sallanmayı durdur
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
    }

    /// <summary>
    /// Mevcut sallanmayı durdurur
    /// </summary>
    public void StopShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        // Shake offset'ini sıfırla
        shakeOffset = Vector3.zero;
        isShaking = false;
    }

    private IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Normalleştirilmiş zaman (0-1)
            float normalizedTime = elapsed / duration;

            // Animasyon eğrisinden güç al
            float curveValue = shakeCurve.Evaluate(normalizedTime);

            // Mevcut sallanma şiddeti
            float currentIntensity = intensity * curveValue;

            // Perlin noise kullanarak yumuşak rastgele değerler oluştur
            float offsetX = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0) - 0.5f) * 2f * currentIntensity;
            float offsetY = (Mathf.PerlinNoise(0, Time.time * shakeFrequency) - 0.5f) * 2f * currentIntensity;

            // Shake offset'ini güncelle (kamera pozisyonunu doğrudan değiştirmek yerine)
            shakeOffset = new Vector3(offsetX, offsetY, 0);

            yield return null;
        }

        // Sallanma bittiğinde offset'i sıfırla
        shakeOffset = Vector3.zero;
        isShaking = false;
        shakeCoroutine = null;
    }

    /// <summary>
    /// Çok güçlü bir sallanma efekti (hasar alma, patlama vb.)
    /// </summary>
    public void StrongShake()
    {
        Shake(3f, 0.8f);
    }

    /// <summary>
    /// Orta şiddette sallanma (saldırı, darbe vb.)
    /// </summary>
    public void MediumShake()
    {
        Shake(1.5f, 0.4f);
    }

    /// <summary>
    /// Hafif sallanma (adım sesi, küçük çarpışma vb.)
    /// </summary>
    public void LightShake()
    {   
        Shake(0.1f, 0.1f);
    }

    /// <summary>
    /// Kamera şu anda sallanıyor mu?
    /// </summary>
    public bool IsShaking()
    {
        return isShaking;
    }

    /// <summary>
    /// Orijinal kamera pozisyonunu yeniden ayarla (sahne değişikliği sonrası kullanılabilir)
    /// </summary>
    public void ResetOriginalPosition()
    {
        // Artık orijinal pozisyon saklamadığımız için bu metod sadece shake'i durdurur
        StopShake();
    }

    /// <summary>
    /// Mevcut shake offset'ini döndürür (debug amaçlı)
    /// </summary>
    public Vector3 GetShakeOffset()
    {
        return shakeOffset;
    }

    // Debug için
    void OnValidate()
    {
        // Inspector'da değer değiştirildiğinde kontrol et
        if (defaultIntensity < 0) defaultIntensity = 0;
        if (defaultDuration < 0) defaultDuration = 0;
        if (shakeFrequency < 1) shakeFrequency = 1;
    }
}