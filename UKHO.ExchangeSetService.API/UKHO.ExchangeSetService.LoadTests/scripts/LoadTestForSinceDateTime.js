import http from 'k6/http';
import { group, check } from 'k6';
import { Trend } from 'k6/metrics';

const logFile = require('../Logging/CaptureLogs.js');
const config = JSON.parse(open('../config.json'));
let SmallExchangeSetTrend = new Trend('SmallEssApiResponseTime');
let LargeExchangeSetTrend = new Trend('LargeEssApiResponseTime');
let MediumExchangeSetTrend = new Trend('MediumEssApiResponseTime');

export function ESSCreation(clientAuthResp, sinceDateTime, exchangeSetType) {
    let essResponse;
    var essUrl = `${config.Base_URL}/productData?sinceDateTime=${sinceDateTime}`;

    group('ESS Api Response', () => {
        essResponse = http.post(encodeURI(essUrl), {}, { headers: { Authorization: `Bearer ${clientAuthResp.essToken}`, "Content-Type": "application/json" } });
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
