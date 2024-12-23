import http from 'k6/http';
import { group, check } from 'k6';
import { Trend } from 'k6/metrics';

const logFile = require('../Logging/CaptureLogs.js');
const config = JSON.parse(open('../config.json'));

let SmallExchangeSetTrend = new Trend('SmallEssApiResponseTime');
let LargeExchangeSetTrend = new Trend('LargeEssApiResponseTime');
let MediumExchangeSetTrend = new Trend('MediumEssApiResponseTime');
let ProductIdentifier = new Trend('ProductIdentifier');
let SinceDate = new Trend('SinceDate');

export function ESSCreation(clientAuthResp, sinceDateTime, exchangeSetType) {
    let essResponse;
    var essUrl = `${config.Base_URL}/productInformation?sinceDateTime=${sinceDateTime}`;
    group('ESS Api Response', () => {
        essResponse = http.get(encodeURI(essUrl), { headers: { Authorization: `Bearer ${clientAuthResp.essToken}`, "Content-Type": "application/json" } });
    });

    check(essResponse, {
        'is ESS status 200': (essResponse) => essResponse.status === 200,
    });
    
    logFile.essConsoleLog(essResponse);

    switch (exchangeSetType) {
        case "Small": SmallExchangeSetTrend.add(essResponse.timings.duration); break;
        case "Medium": MediumExchangeSetTrend.add(essResponse.timings.duration); break;
        case "Large": LargeExchangeSetTrend.add(essResponse.timings.duration); break;
    }
}

export function DeltaSetResponse(clientAuthResp,data, sinceDateTime, exchangeSetType) {
    let productIdentifierResponse, sinceDateTimeResponse;   

    var essUrl = `${config.Base_URL}/${"productInformation/productIdentifiers"}`;  

    group('ProductIdentifier response', () => {
        productIdentifierResponse = http.post(essUrl, JSON.stringify(data), { headers: { Authorization: `Bearer ${clientAuthResp.essToken}`, "Content-Type": "application/json" } });
    });
    check(productIdentifierResponse, {
        'is ProductIdentifier response': (productIdentifierResponse) => productIdentifierResponse.status === 200,
    });
    
    essUrl = `${config.Base_URL}/productInformation?sinceDateTime=${sinceDateTime}`;
    group('sinceDateTimeResponse', () => {
        sinceDateTimeResponse = http.get(encodeURI(essUrl), { headers: { Authorization: `Bearer ${clientAuthResp.essToken}`, "Content-Type": "application/json" } });
    });
    check(sinceDateTimeResponse, {
        'is sinceDateTimeResponse': (sinceDateTimeResponse) => sinceDateTimeResponse.status === 200,
    });

    switch (exchangeSetType) {
        case "Small": ProductIdentifier.add(productIdentifierResponse.timings.waiting); SinceDate.add(sinceDateTimeResponse.timings.waiting); break;
        case "Medium": ProductIdentifier.add(productIdentifierResponse.timings.waiting); SinceDate.add(sinceDateTimeResponse.timings.waiting); break;
        case "Large": ProductIdentifier.add(productIdentifierResponse.timings.waiting ); SinceDate.add(sinceDateTimeResponse.timings.waiting ); break;
    }
}
