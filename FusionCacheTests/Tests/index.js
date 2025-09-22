import http from 'k6/http'
import { check } from 'k6'

export const options = {
    duration: '60s',
    vus: 100
};

export default function () {
    let res = http.get('http://localhost:7000/quotation/PETR4')

    check(res, { 'success': (r) => r.status === 200 })
}
