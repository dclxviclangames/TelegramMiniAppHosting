/*
mergeInto(LibraryManager.library, {
    
    RequestTelegramInvoice: function (unityObjectNamePtr, itemIdentifierPtr, pricePtr) {
        
        const unityObjectName = UTF8ToString(unityObjectNamePtr);
        const itemIdentifier = UTF8ToString(itemIdentifierPtr);
        const price = UTF8ToString(pricePtr);
        
        console.log(`[JSLIB] ������ ��������� �������: ${itemIdentifier} �� ${price} ����.`);

        // --- ������ �������� ������ ---
        setTimeout(() => {
            
            console.log(`[JSLIB] ����� ���������. ����� OnPaymentSuccess � C#.`);
            
            // �������� ����� OnPaymentSuccess � C#
            window.unityInstance.SendMessage('TelegramPaymentBridge', 'OnPaymentSuccess', itemIdentifier);
            
        }, 2000);
    },
    
    // �������� ��� �������������� ������ ������������
    OnPaymentSuccess: function (itemIdentifierPtr) {},
    OnPaymentFailure: function (messagePtr) {}
}); */

mergeInto(LibraryManager.library, {
    
    // ----------------------------------------------------------------------
    // 1. �������: ��������� ID ������������
    // ----------------------------------------------------------------------
    GetTelegramUserId: function () {
        if (Telegram.WebApp && Telegram.WebApp.initDataUnsafe && Telegram.WebApp.initDataUnsafe.user) {
            return allocate(stringToNewUTF8(Telegram.WebApp.initDataUnsafe.user.id.toString()), 'i8', ALLOC_NORMAL);
        }
        return allocate(stringToNewUTF8('0'), 'i8', ALLOC_NORMAL);
    },
    
    // ----------------------------------------------------------------------
    // 2. �������: �������� ������ �� ����
    // ----------------------------------------------------------------------
    OpenTelegramInvoice: function (unityObjectNamePtr, invoiceUrlPtr) {
        
        const unityObjectName = UTF8ToString(unityObjectNamePtr);
        const invoiceUrl = UTF8ToString(invoiceUrlPtr);
        
        console.log(`[JSLIB] �������� ����� �� ������: ${invoiceUrl}`);
        
        Telegram.WebApp.openInvoice(invoiceUrl, function (status) {
            
            // ������ ���� ����� ��������� ������� �����, �� ���������� ������ � C#
            console.log(`[JSLIB] ���� ������ �� ��������: ${status}. �������� � C#.`);
            
            // �������� ����� ����� � C#, ������� �������� ����� �������
            window.unityInstance.SendMessage(unityObjectName, 'HandleInvoiceClosed', status);
        });
    },
    
    // �������� ��� �������������� ������ ������������
    OnPaymentSuccess: function (itemIdentifierPtr) {}, 
    OnPaymentFailure: function (messagePtr) {},
    HandleInvoiceClosed: function (statusPtr) {} // ����� ��������
    
});
