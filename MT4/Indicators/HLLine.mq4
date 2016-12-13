//+------------------------------------------------------------------+
//|                                                       HLLine.mq4 |
//|                                                               rf |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "rf"
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
#property indicator_chart_window

// TODO: show MA

enum mode {
  SMA = MODE_SMA,
  EMA = MODE_EMA,
  // TODO: support swing high low
};
input mode 種別 = SMA;
input int 計算期間 = 20;
input int 表示期間 = 400;
input color 高値線の色 = clrTomato;
input color 安値線の色 = clrAqua;

int ma_method = 種別;
int ma_period = 計算期間;
int indicator_period = 表示期間;
color high_line_color = 高値線の色;
color low_line_color = 安値線の色;

// TODO: use variable length array
int lines_count = 0;
string line_names[];
void add_line_name(string name) {
  arrange_array_size(line_names, lines_count + 1);
  line_names[lines_count++] = name;
}
void arrange_array_size(string &xs[], int size) {
  int as = ArraySize(xs);
  if (size <= as) {
    return;
  }
  ArrayResize(xs, (as == 0) ? 10 : as * 2);
}

void OnInit() {
}

void OnDeinit(const int reason) {
  for (int i = 0; i < lines_count; i++) {
    ObjectDelete(line_names[i]);
  }
}

int OnCalculate(
  const int rates_total,
  const int prev_calculated,
  const datetime &time[],
  const double &open[],
  const double &high[],
  const double &low[],
  const double &close[],
  const long &tick_volume[],
  const long &volume[],
  const int &spread[]
) {
  if (rates_total == prev_calculated) {
    return rates_total;
  }
  int num = (prev_calculated == 0) ? indicator_period : rates_total - prev_calculated;
  State state;
  for (int i = 0; i < num; i++) {
    int index = num - i + 1;
    HL hl = get_hl(index, high[index], low[index]);
    Line out = state.Next(hl, high[index], low[index], time[index]);
    if (out.type != HL_NONE) {
      out.CreateLine();
    }
  }
  return rates_total;
}

enum HL {
  HL_NONE,
  HL_LOW,
  HL_MID,
  HL_HIGH,
};
struct Line {
  HL type;
  double value;
  datetime start;
  datetime end;
  Line(): type(HL_NONE), value(0), start(0), end(0) {}
  Line(HL t, double v, datetime s, datetime e): type(t), value(v), start(s), end(e) {}
  void Set(HL t, double v, datetime s, datetime e) {
    this.type = t;
    this.value = v;
    this.start = s;
    this.end = e;
  }
  void CreateLine() {
    create_line(this.type, this.value, this.start, this.end);
  }
};
struct State {
  HL current;
  HL prev;
  double value;
  datetime start;
  State(): current(HL_NONE), prev(HL_NONE), value(0), start(0) {}
  Line Next(HL hl, double high, double low, datetime time) {
    Line ret;
    // TODO: 3 nested switch is hard to understand
    switch (hl) {
    case HL_HIGH:
      switch (current) {
      case HL_NONE:
        current = HL_HIGH;
        value = high;
        start = time;
        break;
      case HL_HIGH:
        if (value < 0 || value < high) {
          value = high;
        }
        break;
      case HL_MID:
        if (prev == HL_HIGH) {
          ret.Set(HL_LOW, value, start, time);
          start = time;
          value = high;
        } else if (value < 0 || value < high) {
          value = high;
        }
        prev = HL_MID;
        current = HL_HIGH;
        break;
      case HL_LOW:
        if (prev != HL_NONE) {
          ret.Set(HL_LOW, value, start, time);
        }
        prev = HL_LOW;
        current = HL_HIGH;
        start = time;
        value = high;
        break;
      default:
        break;
      }
      break;
    case HL_MID:
      switch (current) {
      case HL_HIGH:
        if (prev != HL_NONE) {
          ret.Set(HL_HIGH, value, start, time);
        }
        prev = HL_HIGH;
        current = HL_MID;
        start = time;
        value = low;
        break;
      case HL_MID:
        switch (prev) {
        case HL_HIGH:
          if (value < 0 || low < value) {
            value = low;
          }
          break;
        case HL_LOW:
          if (value < 0 || value < high) {
            value = high;
          }
          break;
        default:
          break;
        }
        break;
      case HL_LOW:
        if (prev != HL_NONE) {
          ret.Set(HL_LOW, value, start, time);
        }
        prev = HL_LOW;
        current = HL_MID;
        start = time;
        value = high;
        break;
      default:
        break;
      }
      break;
    case HL_LOW:
      switch (current) {
      case HL_NONE:
        current = HL_LOW;
        value = low;
        start = time;
        break;
      case HL_HIGH:
        if (prev != HL_NONE) {
          ret.Set(HL_HIGH, value, start, time);
        }
        prev = HL_HIGH;
        current = HL_LOW;
        start = time;
        value = low;
        break;
      case HL_MID:
        if (prev == HL_LOW) {
          ret.Set(HL_HIGH, value, start, time);
          start = time;
          value = low;
        } else if (value < 0 || low < value) {
          value = low;
        }
        prev = HL_MID;
        current = HL_LOW;
        break;
      case HL_LOW:
        if (value < 0 || low < value) {
          value = low;
        }
        break;
      default:
        break;
      }
      break;
    default:
      break;
    }
    return ret;
  }
};

double get_ma(int index) {
  return iMA(Symbol(), PERIOD_CURRENT, ma_period, 0, ma_method, PRICE_CLOSE, index);
}
HL get_hl(int index, double high, double low) {
  double ma = get_ma(index);
  return (high < ma) ? HL_LOW : (ma < low) ? HL_HIGH : HL_MID;
}

void create_line(HL hl, double value, datetime start, datetime end) {
  if (hl != HL_HIGH && hl != HL_LOW) {
    return;
  }
  color cl = (hl == HL_LOW) ? low_line_color : high_line_color;
  string name = StringFormat("HLLine_%d_%d%d", start, end, hl);
  ObjectCreate(name, OBJ_TREND, 0, start, value, end, value);
  ObjectSet(name, OBJPROP_COLOR, cl);
  ObjectSet(name, OBJPROP_SELECTABLE, false);
  ObjectSet(name, OBJPROP_RAY_RIGHT, false);
  add_line_name(name);
}