using UnityEngine;

namespace RepoTerrainScanner
{
    public class ScanMarker : MonoBehaviour
    {
        private float lifetime = 4.0f;
        private float timer = 0f;
        private Material mat;
        private Color startColor;

        private void Start()
        {
            mat = GetComponent<Renderer>().material;
            startColor = mat.color;
            transform.localScale = Vector3.one * 0.45f;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float t = timer / lifetime;

            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            mat.color = c;
            transform.localScale = Vector3.Lerp(Vector3.one * 0.45f, Vector3.zero, t);
        }

        private void OnDestroy()
        {
            if (mat != null) Destroy(mat);
        }
    }
}
