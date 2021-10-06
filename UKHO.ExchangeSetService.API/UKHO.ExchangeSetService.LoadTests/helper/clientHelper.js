import http from 'k6/http';
import { check, group } from 'k6';
import { Trend } from 'k6/metrics';

const config = JSON.parse(open('../config.json'));
const logFile = require('../logging/captureLogs.js');

let SmallExchangeSetTrendfor25MB = new Trend('SmallEssApiResponsetimefor25MB');
let SmallExchangeSetTrendfor50MB = new Trend('SmallEssApiResponsetimefor50MB');
let MediumExchangeSetTrendfor150MB = new Trend('MediumEssApiResponsetimefor150MB');
let MediumExchangeSetTrendfor300MB = new Trend('MediumEssApiResponsetimeor300MB');
let LargeExchangeSetTrend = new Trend('LargeEssApiResponsetime');

export function GetESSApiResponse(endPoint, data, essToken, exchangeSetType) {
    let essResponse;
    var essUrl = `${config.Base_URL}/productData/${endPoint}`;
    group('ESS Api Response', () => {
        essResponse = http.post(essUrl, JSON.stringify(data), { headers: { Authorization: `Bearer ${essToken}`, "Content-Type": "application/json" } });
    });
    check(essResponse, {
        'is ESS status 200': (essResponse) => essResponse.status === 200,
    });

    logFile.ESSConsoleLog(essResponse);

    switch (exchangeSetType) {
        case "Small_25MB": SmallExchangeSetTrendfor25MB.add(essResponse.timings.waiting); break;
        case "Small_50MB": SmallExchangeSetTrendfor50MB.add(essResponse.timings.waiting); break;
        case "Medium_150MB": MediumExchangeSetTrendfor150MB.add(essResponse.timings.waiting); break;
        case "Medium_300MB": MediumExchangeSetTrendfor300MB.add(essResponse.timings.waiting); break;
        case "Large": LargeExchangeSetTrend.add(essResponse.timings.waiting); break;
    }

    return essResponse;
};

export function GetFSSApiResponse(url, fssToken) {
    let fssResponse = http.get(url, { headers: { Authorization: `Bearer ${fssToken}`, "Content-Type": "application/json" } });

    var batchStatusResponse = JSON.parse(fssResponse.body)
    let fssCommitStatus = JSON.parse(JSON.stringify(batchStatusResponse['status']));

    return fssCommitStatus;
};

export function GetGroupDuration(groupName, f) {
    var start = new Date();
    group(groupName, f);
    return new Date() - start;
}