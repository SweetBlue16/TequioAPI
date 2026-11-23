import { check, sleep } from 'k6';
import http from 'k6/http';

export const options = {
  vus: 5,
  duration: '20s',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1500'],
    checks: ['rate==1'],
  },
};

export default function () {
  const response = http.get('http://localhost:5168/api/v1/products/category/1?pageIndex=1&pageSize=10');
  check(response, {
    'catalog returns 200': (result) => result.status === 200,
    'body contains a products array': (result) => result.status === 200 && Array.isArray(result.json('products')),
  });
  sleep(0.25);
}
