import os
import json
import requests
from flask import Flask, request, jsonify
from telebot import TeleBot

# --- 1. КОНФИГУРАЦИЯ И БАЗА ДАННЫХ ---
BOT_TOKEN = os.environ.get("BOT_TOKEN") 
WEBHOOK_BASE_URL = "https://your-public-server.com" # !!! ЗАМЕНИТЕ !!!
WEBHOOK_PATH = "/telegram_stars_webhook"
TELEGRAM_API_URL = f"https://api.telegram.org/bot{BOT_TOKEN}/"

# ВРЕМЕННАЯ БАЗА ДАННЫХ (в реальном проекте используйте SQLite/PostgreSQL)
# Ключ: user_id (int), Значение: Список купленных item_id (list of str)
PURCHASES = {} 

app = Flask(__name__)
bot = TeleBot(BOT_TOKEN) 

PRODUCTS = {
    "Fighter_Star_1": {
        "title": "Ниндзя-боец (1 Звезда)",
        "description": "Разблокировка элитного бойца",
        "price_stars": 1,
        "payload": "purchase_fighter_star_1" 
    }
}

# ----------------------------------------------------------------------
# ЭНДПОИНТ 1: СОЗДАНИЕ ССЫЛКИ НА СЧЕТ (Вызывается из Unity)
# ----------------------------------------------------------------------
@app.route('/api/create_invoice', methods=['POST'])
def create_invoice():
    data = request.json
    fighter_id = data.get('fighter_id')
    user_id = data.get('user_id') 

    if fighter_id not in PRODUCTS or not user_id:
        return jsonify({"error": "Неверный ID бойца или ID пользователя"}), 400

    product = PRODUCTS[fighter_id]
    
    params = {
        "title": product["title"],
        "description": product["description"],
        "payload": f"{user_id}_{product['payload']}", 
        "currency": "XTR", 
        "prices": json.dumps([{"label": product["title"], "amount": product["price_stars"] * 100}]) 
    }
    
    try:
        response = requests.get(TELEGRAM_API_URL + "createInvoiceLink", params=params)
        response.raise_for_status() 
        result = response.json()
        
        if result.get('ok') and result.get('result'):
            invoice_link = result['result']
            return jsonify({"invoice_url": invoice_link}), 200
        else:
            return jsonify({"error": "Ошибка при создании счета Telegram"}), 500

    except Exception as e:
        return jsonify({"error": "Внутренняя ошибка сервера"}), 500

# ----------------------------------------------------------------------
# ЭНДПОИНТ 2: ВЕБХУК ДЛЯ ПОДТВЕРЖДЕНИЯ ОПЛАТЫ (Вызывается из Telegram)
# ----------------------------------------------------------------------
@app.route(WEBHOOK_PATH, methods=['POST'])
def telegram_webhook():
    update = request.get_json()
    
    if 'message' in update and 'successful_payment' in update['message']:
        payment_info = update['message']['successful_payment']
        chat_id = update['message']['chat']['id']
        payload = payment_info['invoice_payload']
        
        try:
            user_id_str, item_id = payload.split('_', 1)
            user_id = int(user_id_str)
            
            # 1. СОХРАНЕНИЕ В БАЗУ ДАННЫХ (Самый важный шаг)
            if user_id not in PURCHASES:
                PURCHASES[user_id] = []
            
            if item_id not in PURCHASES[user_id]:
                PURCHASES[user_id].append(item_id)
                
            print(f"УСПЕХ WEBHOOK: Пользователь: {user_id}, Товар: {item_id}. База данных обновлена.")
            
            bot.send_message(chat_id, text=f"🎉 Ваша покупка '{item_id}' успешно обработана!")
            
        except Exception as e:
            print(f"Ошибка обработки платежа: {e}")
            
    return 'ok'

# ----------------------------------------------------------------------
# ЭНДПОИНТ 3: ПРОВЕРКА СТАТУСА (Вызывается из Unity для опроса)
# ----------------------------------------------------------------------
@app.route('/api/check_purchase', methods=['POST'])
def check_purchase():
    data = request.json
    try:
        user_id = int(data.get('user_id'))
        item_id = data.get('item_id')
    except (TypeError, ValueError):
        return jsonify({"status": "error", "message": "Invalid user_id"}), 400
    
    # Проверяем, есть ли товар у этого пользователя в нашей "базе"
    is_purchased = user_id in PURCHASES and item_id in PURCHASES.get(user_id, [])
    
    return jsonify({
        "status": "success",
        "purchased": is_purchased,
        "item_id": item_id
    }), 200


# ----------------------------------------------------------------------
# НАСТРОЙКА СЕРВЕРА
# ----------------------------------------------------------------------
@app.route("/set_webhook")
def set_webhook_route():
    # ... (код установки вебхука) ...
    pass 

if __name__ == '__main__':
    port = int(os.environ.get('PORT', 5000))
    app.run(host='0.0.0.0', port=port)
