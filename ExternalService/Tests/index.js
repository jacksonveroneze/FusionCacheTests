import http from "k6/http";
import { sleep, check } from "k6";
import exec from "k6/execution";

// ... existing code ...

export default function () {
    // ... existing code ...

    const baseUrl = "http://localhost:8086";
    const url = `${baseUrl}/quotations/PETR4?timeout=5000`;

    const params = {
        timeout: "1s",
    };

    const res = http.get(url, params);

    check(res, {
        success: (r) => r.status === 200,
        timedOutOrAborted: (r) => r.status === 0,
    });

    sleep(0.2);
}