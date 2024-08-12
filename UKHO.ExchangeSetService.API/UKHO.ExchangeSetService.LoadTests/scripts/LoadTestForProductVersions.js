import { check, group } from 'k6';
import http from 'k6/http';
import { Trend } from 'k6/metrics';

const config = JSON.parse(open('../config.json'));
const logFile = require('../Logging/CaptureLogs.js');

let SmallExchangeSetTrend = new Trend('SmallEssApiResponsetime');
let LargeExchangeSetTrend = new Trend('LargeEssApiResponsetime');
let MediumExchangeSetTrend = new Trend('MediumEssApiResponsetime');

export function ESSCreation(clientAuthResp, productVersionData, exchangeSetType) {
    let essResponse;
    let endPoint ="productVersions"
    var essUrl = `${config.Base_URL}/productData/${endPoint}`;

    group('ESS Api Response', () => {
        essResponse = http.post(essUrl, JSON.stringify(productVersionData), { headers: { Authorization: `Bearer ${clientAuthResp.essToken}`, "Content-Type": "application/json" } });
    });
    check(essResponse, {
        'is ESS status 200': (essResponse) => essResponse.status === 200,
    });

    logFile.essConsoleLog(essResponse);

    switch (exchangeSetType) {
        case "Small": SmallExchangeSetTrend.add(essResponse.timings.waiting); break;
        case "Medium": MediumExchangeSetTrend.add(essResponse.timings.waiting); break;
        case "Large": LargeExchangeSetTrend.add(essResponse.timings.waiting); break;
    }
}