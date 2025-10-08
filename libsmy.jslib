/*
mergeInto(LibraryManager.library, {
    
    RequestTelegramInvoice: function (unityObjectNamePtr, itemIdentifierPtr, pricePtr) {
        
        const unityObjectName = UTF8ToString(unityObjectNamePtr);
        const itemIdentifier = UTF8ToString(itemIdentifierPtr);
        const price = UTF8ToString(pricePtr);
        
        console.log(`[JSLIB] Начало симуляции покупки: ${itemIdentifier} за ${price} Звёзд.`);

        // --- ЛОГИКА ФЕЙКОВОЙ ОПЛАТЫ ---
        setTimeout(() => {
            
            console.log(`[JSLIB] Успех симуляции. Вызов OnPaymentSuccess в C#.`);
            
            // Вызываем метод OnPaymentSuccess в C#
            window.unityInstance.SendMessage('TelegramPaymentBridge', 'OnPaymentSuccess', itemIdentifier);
            
        }, 2000);
    },
    
    // Заглушки для предотвращения ошибок компоновщика
    OnPaymentSuccess: function (itemIdentifierPtr) {},
    OnPaymentFailure: function (messagePtr) {}
}); */

mergeInto(LibraryManager.library, {
    
    // ----------------------------------------------------------------------
    // 1. ФУНКЦИЯ: ПОЛУЧЕНИЕ ID ПОЛЬЗОВАТЕЛЯ
    // ----------------------------------------------------------------------
    GetTelegramUserId: function () {
        if (Telegram.WebApp && Telegram.WebApp.initDataUnsafe && Telegram.WebApp.initDataUnsafe.user) {
            return allocate(stringToNewUTF8(Telegram.WebApp.initDataUnsafe.user.id.toString()), 'i8', ALLOC_NORMAL);
        }
        return allocate(stringToNewUTF8('0'), 'i8', ALLOC_NORMAL);
    },
    
    // ----------------------------------------------------------------------
    // 2. ФУНКЦИЯ: ОТКРЫТИЕ ССЫЛКИ НА СЧЕТ
    // ----------------------------------------------------------------------
    OpenTelegramInvoice: function (unityObjectNamePtr, invoiceUrlPtr) {
        
        const unityObjectName = UTF8ToString(unityObjectNamePtr);
        const invoiceUrl = UTF8ToString(invoiceUrlPtr);
        
        console.log(`[JSLIB] Открытие счета по ссылке: ${invoiceUrl}`);
        
        Telegram.WebApp.openInvoice(invoiceUrl, function (status) {
            
            // Вместо того чтобы принимать решение здесь, мы отправляем статус в C#
            console.log(`[JSLIB] Счет закрыт со статусом: ${status}. Передача в C#.`);
            
            // Вызываем новый метод в C#, который запустит опрос сервера
            window.unityInstance.SendMessage(unityObjectName, 'HandleInvoiceClosed', status);
        });
    },
    
    // Заглушки для предотвращения ошибок компоновщика
    OnPaymentSuccess: function (itemIdentifierPtr) {}, 
    OnPaymentFailure: function (messagePtr) {},
    HandleInvoiceClosed: function (statusPtr) {} // Новая заглушка
    
});
