using UnityEngine;
using UnityEngine.UI;

// Genera burbujas circulares decorativas que flotan en bucle.
// Agregar como componente al Image de fondo oscuro del Canvas de pausa.
public class PauseBubbles : MonoBehaviour
{
    [Header("Configuración")]
    public int cantidadBurbujas = 15;
    public float velocidad = 100f;
    public float tamanoMin = 20f;
    public float tamanoMax = 55f;
    public Color colorBurbuja = new Color(0.5f, 0.8f, 1f, 0.18f);

    private RectTransform parentRect;
    private Sprite circleSprite;

    void OnEnable()
    {
        parentRect = GetComponent<RectTransform>();
        circleSprite = CrearCirculo();

        // Limpiar burbujas anteriores (por si se reactiva)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (transform.GetChild(i).name.StartsWith("Bubble"))
                Destroy(transform.GetChild(i).gameObject);
        }

        // Crear todas las burbujas de una vez, distribuidas por la pantalla
        for (int i = 0; i < cantidadBurbujas; i++)
            SpawnBurbuja(true);
    }

    void Update()
    {
        float halfH = parentRect.rect.height * 0.5f;

        // Mover cada burbuja hacia arriba
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith("Bubble")) continue;

            BubbleData data = child.GetComponent<BubbleData>();
            if (data == null) continue;

            RectTransform rect = child.GetComponent<RectTransform>();
            data.timer += Time.unscaledDeltaTime;

            float y = rect.anchoredPosition.y + data.speed * Time.unscaledDeltaTime;
            float x = data.startX + Mathf.Sin(data.timer * data.wobbleSpeed) * data.wobbleAmount;
            rect.anchoredPosition = new Vector2(x, y);

            // Bucle: si sale por arriba, reaparece abajo
            if (y > halfH + data.size)
            {
                float newX = Random.Range(-parentRect.rect.width * 0.45f, parentRect.rect.width * 0.45f);
                data.startX = newX;
                data.timer = 0f;
                rect.anchoredPosition = new Vector2(newX, -halfH - data.size);
            }
        }
    }

    void SpawnBurbuja(bool posicionAleatoria)
    {
        GameObject bubble = new GameObject("Bubble", typeof(RectTransform), typeof(Image), typeof(BubbleData));
        bubble.transform.SetParent(transform, false);

        float size = Random.Range(tamanoMin, tamanoMax);

        RectTransform rect = bubble.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);

        float halfW = parentRect.rect.width * 0.45f;
        float halfH = parentRect.rect.height * 0.5f;
        float x = Random.Range(-halfW, halfW);
        // Si es la creación inicial, distribuir por toda la pantalla
        float y = posicionAleatoria ? Random.Range(-halfH, halfH) : -halfH - size;
        rect.anchoredPosition = new Vector2(x, y);

        Image img = bubble.GetComponent<Image>();
        img.sprite = circleSprite;
        img.color = new Color(
            colorBurbuja.r + Random.Range(-0.05f, 0.05f),
            colorBurbuja.g + Random.Range(-0.05f, 0.05f),
            colorBurbuja.b + Random.Range(-0.02f, 0.02f),
            Random.Range(0.06f, colorBurbuja.a)
        );
        img.raycastTarget = false;

        BubbleData data = bubble.GetComponent<BubbleData>();
        data.speed = velocidad + Random.Range(-30f, 30f);
        data.wobbleAmount = Random.Range(15f, 45f);
        data.wobbleSpeed = Random.Range(1.5f, 3f);
        data.startX = x;
        data.size = size;
        data.timer = Random.Range(0f, 6f); // Fase aleatoria para que no se muevan igual
    }

    // Genera un sprite circular blanco en memoria (32x32)
    Sprite CrearCirculo()
    {
        int res = 32;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float center = res * 0.5f;
        float radius = center - 1f;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                // Borde suave
                float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }
}

// Almacena datos de movimiento de cada burbuja
public class BubbleData : MonoBehaviour
{
    [HideInInspector] public float speed;
    [HideInInspector] public float wobbleAmount;
    [HideInInspector] public float wobbleSpeed;
    [HideInInspector] public float startX;
    [HideInInspector] public float size;
    [HideInInspector] public float timer;
}
