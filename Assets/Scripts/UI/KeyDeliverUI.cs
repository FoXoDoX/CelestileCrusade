using UnityEngine;
using UnityEngine.UI;

public class KeyDeliverUI : MonoBehaviour
{
    [SerializeField] private Image keyFilledImage;
    [SerializeField] private KeyDeliver keyDeliver;
    [SerializeField] private SpriteRenderer keySpriteRenderer;
    [SerializeField] private SpriteRenderer crossSpriteRenderer;
    [SerializeField] private Key.KeyType keyType;

    private KeyHolder keyHolder;

    private void Start()
    {
        Lander.Instance.OnKeyDeliver += Lander_OnKeyDeliver;

        if (Lander.Instance != null)
        {
            keyHolder = Lander.Instance.GetComponent<KeyHolder>();
        }

        UpdateKeySprites();
    }

    private void Lander_OnKeyDeliver(object sender, Lander.KeyDeliverEventArgs e)
    {
        UpdateKeySprites();
    }

    private void Update()
    {
        if (keyDeliver != null)
        {
            keyFilledImage.fillAmount = keyDeliver.GetDeliverProgress();
        }

        UpdateKeySprites();
    }

    private void UpdateKeySprites()
    {
        if (keyHolder != null)
        {
            bool hasKey = keyHolder.ContainsKey(keyType);

            if (keySpriteRenderer != null)
                keySpriteRenderer.gameObject.SetActive(hasKey);

            if (crossSpriteRenderer != null)
                crossSpriteRenderer.gameObject.SetActive(!hasKey);
        }
    }
}