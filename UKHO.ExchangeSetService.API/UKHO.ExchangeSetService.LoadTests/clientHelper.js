import http from 'k6/http';
import { check, group } from 'k6';
import { Trend } from 'k6/metrics';

const config = JSON.parse(open('./config.json'));

let SmallExchangeSetTrend = new Trend('SmallEssApiResponsetime');
let LargeExchangeSetTrend = new Trend('LargeEssApiResponsetime');
let MediumExchangeSetTrend = new Trend('MediumEssApiResponsetime');

export function GetESSApiResponse(endPoint, data, essToken, exchangeSetType) {
    let essResponse;
    var essUrl = `${config.Base_URL}/productData/${endPoint}`;
    group('ESS Api Response', () => {
        essResponse = http.post(essUrl, JSON.stringify(data), { headers: { Authorization: `Bearer ${essToken}`, "Content-Type": "application/json" } });
    });
    check(essResponse, {
        'is ESS status 200': (essResponse) => essResponse.status === 200,
    });

    switch (exchangeSetType) {
        case "Small": SmallExchangeSetTrend.add(essResponse.timings.waiting); break;
        case "Medium": MediumExchangeSetTrend.add(essResponse.timings.waiting); break;
        case "Large": LargeExchangeSetTrend.add(essResponse.timings.waiting); break;
    }

    let jsonResponse = JSON.parse(essResponse.body);
    let batchStatusUrl = JSON.stringify(jsonResponse['_links']['exchangeSetBatchStatusUri']['href']);

    return batchStatusUrl;
};

export function GetESSApiResponseForSinceDateTime(sinceDateTime, essToken, exchangeSetType) {
    let essResponse;
    var essUrl = `${config.Base_URL}/productData?sinceDateTime=${sinceDateTime}`;
    group('ESS Api Response', () => {
        essResponse = http.post(essUrl, {}, { headers: { Authorization: `Bearer ${essToken}` } });
    });
    check(essResponse, {
        'is ESS status 200': (essResponse) => essResponse.status === 200,
    });

    switch (exchangeSetType) {
        case "Small": SmallExchangeSetTrend.add(essResponse.timings.waiting); break;
        case "Medium": MediumExchangeSetTrend.add(essResponse.timings.waiting); break;
        case "Large": LargeExchangeSetTrend.add(essResponse.timings.waiting); break;
    }

    let jsonResponse = JSON.parse(essResponse.body);
    let batchStatusUrl = JSON.stringify(jsonResponse['_links']['exchangeSetBatchStatusUri']['href']);

    return batchStatusUrl;
}

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