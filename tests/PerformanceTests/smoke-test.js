import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
  vus: 10,
  duration: '10s',
  thresholds: {
    http_req_duration: ['p(95)<300'], // 95% of requests must complete below 300ms
  },
};

export default function () {
  // Test Liveness
  const resLive = http.get('http://localhost:5000/api/health/live');
  check(resLive, { 'is status 200': (r) => r.status === 200 });

  // Test Readiness
  const resReady = http.get('http://localhost:5000/api/health/ready');
  check(resReady, { 'is status 200': (r) => r.status === 200 });

  // Test a basic public endpoint if any exist, e.g., the /api/admin/sources if we had authentication set up.
  // For now, testing health checks is sufficient to establish a baseline of the ASP.NET Core request pipeline.

  sleep(1);
}
