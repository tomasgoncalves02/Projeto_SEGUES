import { DOM } from "../core/dom";

function clearData(ids) {
    ids.forEach(id => {
        const el = DOM.byId(id);
        if (el) el.textContent = '...';
    });
}

const Statistics = { 
    init() {
        DOM.bind('selectTicketsPeriod', 'change', loadMealsSummary);
        DOM.bind('selectOrdersPeriod', 'change', loadBarSummary);
    } 
};