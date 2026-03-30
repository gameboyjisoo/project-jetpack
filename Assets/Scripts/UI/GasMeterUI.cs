using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays jetpack gas as a fill bar on the UI.
/// </summary>
public class GasMeterUI : MonoBehaviour
{
    [SerializeField] private Image gasFillBar;
    [SerializeField] private Color fullColor = new Color(0.2f, 0.8f, 1f);
    [SerializeField] private Color emptyColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private float smoothSpeed = 8f;

    private JetpackGas jetpackGas;
    private float displayPercent = 1f;

    private void Start()
    {
        jetpackGas = FindFirstObjectByType<JetpackGas>();
    }

    private void Update()
    {
        if (jetpackGas == null || gasFillBar == null) return;

        float targetPercent = jetpackGas.GasPercent;
        displayPercent = Mathf.Lerp(displayPercent, targetPercent, smoothSpeed * Time.deltaTime);

        gasFillBar.fillAmount = displayPercent;
        gasFillBar.color = Color.Lerp(emptyColor, fullColor, displayPercent);
    }
}
