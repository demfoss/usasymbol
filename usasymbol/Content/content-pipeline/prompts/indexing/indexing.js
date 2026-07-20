const { GoogleAuth } = require('google-auth-library');
const axios = require('axios');
const fs = require('fs');

const SERVICE_ACCOUNT_FILE = './service_account.json';
const SCOPES = ['https://www.googleapis.com/auth/indexing'];

const URLS = fs.readFileSync('urls.txt', 'utf-8')
    .split('\n')
    .map(url => url.trim())
    .filter(url => url.length > 0);

async function getAccessToken() {
    const auth = new GoogleAuth({
        keyFile: SERVICE_ACCOUNT_FILE,
        scopes: SCOPES,
    });
    const client = await auth.getClient();
    const tokenResponse = await client.getAccessToken();
    return tokenResponse.token;
}

async function indexUrl(token, url) {
    const endpoint = 'https://indexing.googleapis.com/v3/urlNotifications:publish';
    const response = await axios.post(endpoint, 
        { url, type: 'URL_UPDATED' },
        { 
            headers: { 
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            validateStatus: null // чтобы не бросал исключение на 4xx
        }
    );
    return { status: response.status, data: response.data };
}

async function main() {
    console.log('Получаем токен...');
    const token = await getAccessToken();
    console.log('Токен получен!\n');

    const results = { success: [], error: [] };

    for (const url of URLS) {
        const { status, data } = await indexUrl(token, url);
        if (status === 200) {
            console.log(`✓ OK: ${url}`);
            results.success.push(url);
        } else {
            const errorMsg = data?.error?.message || 'Unknown error';
            console.log(`✗ Ошибка ${status}: ${url} — ${errorMsg}`);
            results.error.push({ url, error: errorMsg });
        }
    }

    console.log('\n=== Итог ===');
    console.log(`Успешно: ${results.success.length}`);
    console.log(`Ошибок:  ${results.error.length}`);

    fs.writeFileSync('indexing_results.json', JSON.stringify(results, null, 2));
    console.log('Результаты сохранены в indexing_results.json');
}

main().catch(console.error);