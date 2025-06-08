import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
    stages: [
        { duration: '1m', target: 0 },
        { duration: '2m', target: 10 },
        { duration: '2m', target: 20 },
        { duration: '2m', target: 30 },
        { duration: '2m', target: 40 },
        { duration: '2m', target: 80 },
        { duration: '2m', target: 100 },
        { duration: '2m', target: 50 },
        { duration: '5m', target: 10 },
        { duration: '5m', target: 5 },
        { duration: '2m', target: 0 },
    ],
};

export default function () {
    const url = 'http://auth/UserAuth/login';
    const payload = JSON.stringify({
        UserEmail: '1234@1234.1234',
        Password: '1234@1234.1234'
    });

    const headers = { 'Content-Type': 'application/json' };

    const res = http.post(url, payload, { headers });

    check(res, {
        'status is 200': (r) => r.status === 200,
        'token received': (r) => !!r.json('JWTToken'),
        'response time < 500ms': (r) => r.timings.duration < 500,
    });

    sleep(1);
}
