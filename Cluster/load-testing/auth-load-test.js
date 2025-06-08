import http from 'k6/http';
import { sleep, check } from 'k6';

export let options = {
    stages: [
        { duration: '1m', target: 300 },   // ramp-up to 300 VUs over 1 minute
        { duration: '3m', target: 300 },   // stay at 300 VUs for 3 minutes
        { duration: '1m', target: 0 },     // ramp-down to 0
    ],
};

export default function () {
    const url = 'http://auth/UserAuth/login';
    const payload = JSON.stringify({
        UserEmail: '1234@1234.1234',
        Password: '1234@1234.1234'
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    const res = http.post(url, payload, params);

    check(res, {
        'status is 200': (r) => r.status === 200,
        'token received': (r) => r.json('JWTToken') !== undefined,
    });

    sleep(1);
}
