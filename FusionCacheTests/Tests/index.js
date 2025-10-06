import http from 'k6/http'
import { check } from 'k6'

export const options = {
    duration: '240s',
    vus: 250
};

export default function () {
    let res = http.get('http://localhost:8080/cache-tests/quotation-distrib/PETR4')
    // let res = http.get('http://localhost:8080/cache-tests/quotation-fusion/PETR4')
    // let res = http.get('http://localhost:7000/quotation-fusion/PETR4')

    check(res, { 'success': (r) => r.status === 200 })
}
