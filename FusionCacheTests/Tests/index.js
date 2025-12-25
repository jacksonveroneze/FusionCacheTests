import http from "k6/http";
import {sleep, check} from "k6";
import exec from "k6/execution";

export const options = {
    scenarios: {
        load: {
            executor: "ramping-vus",
            startVUs: 0,
            stages: [
                {duration: "5s", target: 5},
                {duration: "55s", target: 50},
                {duration: "60s", target: 100},
                {duration: "60s", target: 100},
                {duration: "60s", target: 200},
                {duration: "60s", target: 200},
            ],
            gracefulRampDown: "0s",
        },
    },
};

export default function () {
    const ids = [
        "e6aa15f4-4625-49e5-a2c7-9aef93936dd1",
        "e6aa15f4-4625-49e5-a2c7-9aef93936dd2",
        "e6aa15f4-4625-49e5-a2c7-9aef93936dd3",
        "e6aa15f4-4625-49e5-a2c7-9aef93936dd4",
        "e6aa15f4-4625-49e5-a2c7-9aef93936dd5"];

    const random = Math.floor(Math.random() * ids.length);
    const contentId = ids[random]

    const elapsedMs = Date.now() - exec.scenario.startTime;

    const t = elapsedMs / 1000;

    let faultMode = "normal";
    const inErrorWindow =
        (t >= 80 && t < 130) ||
        (t >= 200 && t < 230);

    if (inErrorWindow) faultMode = "error";

    const baseUrl = "http://localhost:7000";

    const url = `${baseUrl}/bff-content?contentId=${contentId}&faultMode=${faultMode}&useFusion=true`;

    var res = http.get(url);

    check(res, { 'success': (r) => r.status === 200 })
    
    sleep(0.2);
}
