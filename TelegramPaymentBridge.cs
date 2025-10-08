using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Networking;
using System.Collections;
using LitJson; // ���������� LitJson

public class TelegramPaymentBridge : MonoBehaviour
{
    private const string UNITY_OBJECT_NAME = "TelegramPaymentBridge";
    public GameObject requestPanel;

    // !!! �������� �� ��� �������� URL Render-������� !!!
    private const string API_BASE_URL = "https://your-public-server.com/api";

    private string lastRequestedFighterId;
    private string lastRequestedUserId;

    // 1. ���������� ��� JSLIB
    [DllImport("__Internal")]
    private static extern string GetTelegramUserId();

    [DllImport("__Internal")]
    private static extern void OpenTelegramInvoice(string unityObjectName, string invoiceUrl);

    // ----------------------------------------------------------------------
    // 2. ��������� � ��������� ������
    // ----------------------------------------------------------------------
    public void PurchaseFighter(string fighterID)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string userId = GetTelegramUserId(); 
        
        if (string.IsNullOrEmpty(userId) || userId == "0")
        {
            Debug.LogError("������: �� ������� �������� ID ������������ Telegram.");
            return;
        }

        lastRequestedFighterId = fighterID; // ��������� ID ������ ��� ������
        lastRequestedUserId = userId; // ��������� ID ������������ ��� ������
        
        Debug.Log($"[C#] ������ ������ ��� {fighterID} (User ID: {userId})");
        
        StartCoroutine(RequestInvoiceLink(fighterID, userId));
#else
        Debug.Log("[C#] ������ ������ � ��������� ��������.");
#endif
    }

    IEnumerator RequestInvoiceLink(string fighterID, string userId)
    {
        string jsonPayload = $"{{\"fighter_id\": \"{fighterID}\", \"user_id\": \"{userId}\"}}";

        UnityWebRequest webRequest = UnityWebRequest.PostWwwForm(API_BASE_URL + "/create_invoice", "POST"); // ����������� URL
        webRequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonPayload));
        webRequest.SetRequestHeader("Content-Type", "application/json");

        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string responseText = webRequest.downloadHandler.text;
            string invoiceUrl = ParseInvoiceUrl(responseText);

            if (!string.IsNullOrEmpty(invoiceUrl))
            {
                // �������� JSLIB ��� �������� �����
                OpenTelegramInvoice(UNITY_OBJECT_NAME, invoiceUrl);
            }
            else
            {
                Debug.LogError("�� ������� �������� ������ �� ���� �� Python-�������: " + responseText);
            }
        }
        else
        {
            Debug.LogError($"������ HTTP-������� � �������: {webRequest.error}");
        }
    }

    // ----------------------------------------------------------------------
    // 3. ��������� �������� ����� (���������� �� JSLIB)
    // ----------------------------------------------------------------------

    // JSLIB �������� ���� �����, ����� ���� ������ ���������.
    public void HandleInvoiceClosed(string status)
    {
        if (status == "paid")
        {
            // ������������ �������. �������� ���������� �������� (Polling)
            Debug.Log("[C#] ������ ��������� (������). �������� �������� ������� �� �������...");
            StartCoroutine(PollForPurchaseConfirmation(lastRequestedUserId, lastRequestedFighterId));
        }
        else if (status == "cancelled")
        {
            OnPaymentFailure("�������� �������������.");
        }
        else
        {
            OnPaymentFailure($"����������� ������: {status}");
        }
    }

    // ----------------------------------------------------------------------
    // 4. ���������� ���� ������ (Polling)
    // ----------------------------------------------------------------------
    IEnumerator PollForPurchaseConfirmation(string userId, string itemId)
    {
        int maxAttempts = 10; // 10 ������� � ���������� � 2 �������

        for (int i = 0; i < maxAttempts; i++)
        {
            string jsonPayload = $"{{\"user_id\": \"{userId}\", \"item_id\": \"{itemId}\"}}";

            // ���������� ����� �������� ��� �������� �������
            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm(API_BASE_URL + "/check_purchase", "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonPayload));
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                var data = JsonMapper.ToObject(webRequest.downloadHandler.text);
                bool isPurchaused = (bool)data["purchased"];
                if(isPurchaused)
                {
                    // ������������� �� �������! ��������� ������������ �����.
                    OnPaymentSuccess(itemId);
                    yield break; // ������� �� �����
                }
            }

            Debug.Log($"[Polling] ������� �� ������������. ������� {i + 1}/{maxAttempts}");
            yield return new WaitForSeconds(2f);
        }

        // ���� ����� �������� �������
        OnPaymentFailure("Confirmation Timeout: ������ �� ���������� ������� �������.");
    }

    // ----------------------------------------------------------------------
    // 5. �������� ������ (������������� UI)
    // ----------------------------------------------------------------------
    private string ParseInvoiceUrl(string jsonText)
    {
        try
        {
            // Парсим JSON
            var data = JsonMapper.ToObject(jsonText);

            // Пытаемся получить значение по ключу. Если ключа нет, будет брошено исключение.
            string invoiceUrl = data["invoice_url"].ToString();

            if (!string.IsNullOrEmpty(invoiceUrl))
            {
                return invoiceUrl;
            }

            Debug.LogError("Ошибка парсинга: ключ 'invoice_url' найден, но пуст.");
            return null;
        }
        catch (System.Exception ex)
        {
            // Если ключ не существует или произошла ошибка парсинга
            Debug.LogError($"Ошибка парсинга JSON (LitJson). Ключ 'invoice_url' отсутствует: {ex.Message}");
            return null;
        }
    }

    public void OnPaymentSuccess(string itemIdentifier)
    {
        Debug.Log($" [C#] ��������� �����! ����� �������������: {itemIdentifier}");
        // ����� ���� ������ ������������� UI/������
        if (itemIdentifier == "Fighter_Star_1")
        {
            requestPanel.SetActive(false);
        }
    }

    public void OnPaymentFailure(string message)
    {
        Debug.LogWarning($" [C#] ������ �� ������/�������: {message}");
        // ����� ���� ������ ������ UI
    }
}
