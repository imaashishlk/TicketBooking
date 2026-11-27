import http from 'k6/http';
import { check } from 'k6';
import { Counter } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL;
const EVENT_ID = Number(__ENV.EVENT_ID);

const successfulBookings = new Counter('successful_bookings');
const soldOutBookings = new Counter('sold_out_bookings');
const unexpectedErrors = new Counter('unexpected_errors');
const temporarilyUnavailable = new Counter('unavailable_errors');

// Expected statuses, from the backend 
http.setResponseCallback(http.expectedStatuses(201, 409));

// Creating 100 VUs, with each sending 1 request each (iterations), for the max duration of 30 seconds
export const options = {
    scenarios: {
        booking_test: {
            executor: 'per-vu-iterations',
            vus: 100,
            iterations: 1,
            maxDuration: '30s',
        },
    },
};

// Getting the loadtest users from the DB
export function setup() {
    const response = http.get(`${BASE_URL}/api/users`, {
        responseCallback: http.expectedStatuses(200),
    });

    const users = response.json()
        .filter(u => u.email.startsWith('loadtest'));

    if (users.length < 100) {
        throw new Error('At least 100 test users are required.');
    }

    return users.slice(0, 100);
}

// Creating requests and hitting, calculating successfull bookings 201,
// 409 for conflicts or sold outs
// only two responses are expected so checking the responses
export default function (users) {
    const payload = JSON.stringify({
        userId: users[__VU - 1].id,
        eventId: EVENT_ID,
    });

    const response = http.post(
        `${BASE_URL}/api/bookings`,
        payload,
        {
            headers: {
                'Content-Type': 'application/json',
            },
        }
    );

    successfulBookings.add(response.status === 201 ? 1 : 0);
    soldOutBookings.add(response.status === 409 ? 1 : 0);
    temporarilyUnavailable.add(response.status === 503 ? 1 : 0);
    unexpectedErrors.add(
        response.status !== 201 && response.status !== 409 && response.status !== 503 ? 1 : 0
    );

    check(response, {
        'valid booking response': (r) =>
            r.status === 201 || r.status === 409 || r.status === 503,
    });
}