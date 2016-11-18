#property copyright "rf"
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict

int OnInit() {
  ChartSetInteger(ChartID(), CHART_EVENT_OBJECT_CREATE, true);
  ChartSetInteger(ChartID(), CHART_EVENT_OBJECT_DELETE, true);
  send_to_server(init_data());
  return INIT_SUCCEEDED;
}
void OnDeinit(const int reason) {
  send_to_server(close_data());
}
void OnTick() {
  send_to_server(tick_data());
}
void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam) {
  if (chart_object_changed(id)) {
    send_to_server(line_changed_data());
  }
}

int send_to_server(string data) {
  char body[];
  char res[];
  string rh;
  StringToCharArray(data, body);
  int ret = WebRequest("POST", "http://localhost/", "", 300, body, res, rh);
  switch (ret) {
  case -1:
    Print("send_to_server failed.");
    break;
  case 200:
    break;
  case 412:
    Print("send_to_server failed - server required init. send init.");
    return send_to_server(init_data());
  default:
    Print("send_to_server failed - server responed ", ret);
    break;
  }
  return ret;
}

string init_data() {
  return StringFormat("{\"symbol\":\"%s\",\"chart\":%lld,\"operation\":\"init\",\"price\":%s,\"lines\":%s}", Symbol(), ChartID(), price(), lines());
}
string tick_data() {
  return StringFormat("{\"symbol\":\"%s\",\"chart\":%lld,\"operation\":\"tick\",\"price\":%s}", Symbol(), ChartID(), price());
}


string line_changed_data() {
  return StringFormat("{\"symbol\":\"%s\",\"chart\":%lld,\"operation\":\"line\",\"lines\":%s}", Symbol(), ChartID(), lines());
}
string close_data() {
  return StringFormat("{\"symbol\":\"%s\",\"chart\":%lld,\"operation\":\"close\"}", Symbol(), ChartID());
}

string price() {
  MqlTick tick;
  SymbolInfoTick(Symbol(), tick);
  return StringFormat("{\"ask\":%s,\"bid\":%s,\"time\":%d}", DoubleToString(Ask, Digits), DoubleToString(Bid, Digits), utc_time(tick.time));
}
datetime utc_time(datetime t) {
  datetime c = TimeCurrent();
  datetime g = TimeGMT();
  return t - c + g;
}

bool chart_object_changed(const int id) {
  return id == CHARTEVENT_OBJECT_CREATE
    || id == CHARTEVENT_OBJECT_DELETE
    || id == CHARTEVENT_OBJECT_CHANGE
    || id == CHARTEVENT_OBJECT_DRAG;
}

string lines() {
  string ls = "";
  for (int i = 0; i < ObjectsTotal(ChartID()); i++) {
    string name = ObjectName(i);
    switch (ObjectType(name)) {
    case OBJ_HLINE: {
      string desc = ObjectGetString(ChartID(), name, OBJPROP_TEXT);
      double price = ObjectGetDouble(ChartID(), name, OBJPROP_PRICE1);
      long clr = ObjectGetInteger(ChartID(), name, OBJPROP_COLOR);
      string line = StringFormat("{\"name\":\"%s\",\"price\":%s,\"color\":%d,\"description\":\"%s\"},", name, DoubleToString(price, Digits), clr, desc);
      StringAdd(ls, line);
      break;
    }
    case OBJ_TREND: {
      string desc = ObjectGetString(ChartID(), name, OBJPROP_TEXT);
      double price1 = ObjectGetDouble(ChartID(), name, OBJPROP_PRICE1);
      double price2 = ObjectGetDouble(ChartID(), name, OBJPROP_PRICE2);
      if (price1 == price2) {
        long clr = ObjectGetInteger(ChartID(), name, OBJPROP_COLOR);
        string line = StringFormat("{\"name\":\"%s\",\"price\":%s,\"color\":%d,\"description\":\"%s\"},", name, DoubleToString(price1, Digits), clr, desc);
        StringAdd(ls, line);
      }
      break;
    }
    default:
      break;
    }
  }
  if (ls != "") {
    ls = StringSubstr(ls, 0, StringLen(ls) - 1);
  }
  return StringConcatenate("[", ls, "]");
}
