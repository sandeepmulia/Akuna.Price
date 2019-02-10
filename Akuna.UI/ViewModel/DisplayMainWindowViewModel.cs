using Akuna.UI.Interface;
using Akuna.UI.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Akuna.PriceService;
using System.Collections.Specialized;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Collections.Concurrent;

namespace Akuna.UI.ViewModel
{
    class DisplayMainWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public ICommand StartCommand { get; set; }
        public ICommand StopCommand { get; set; }
        private bool toggleButton { get; set; }
        private static readonly object _syncLock = new object();
        private static readonly object _syncLockDelta = new object();
        private RandomWalkPriceService priceService;

        private ObservableDictionary<uint, Instrument> _instrumentsCache = new ObservableDictionary<uint, Instrument>();
        public ObservableDictionary<uint, Instrument> Instruments
        {
            get
            {
                return _instrumentsCache;
            }
            set
            {
                _instrumentsCache = value;
            }
        }

        private List<DeltaIndicator> _deltaIndicator = new List<Model.DeltaIndicator>();
        public List<DeltaIndicator> DeltaIndicator { get; set; }
        private BlockingCollection<Instrument> _queue = new BlockingCollection<Instrument>();
        private Task _consumer;
        private Task _producer;
        private CancellationTokenSource _token;
        private System.Timers.Timer _timer = new System.Timers.Timer();
        private volatile bool _resetFlag = true;

        public DisplayMainWindowViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(_instrumentsCache, _syncLock);
            toggleButton = true;
            StartCommand = new DelegateCommand(StartPriceService, EnableStartButton);
            StopCommand = new DelegateCommand(StopPriceService, EnableStopButton);
            priceService = new RandomWalkPriceService();
            StartConsumer();
            StartTimer();
        }

        private void StartTimer()
        {
            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = 5000;
            _timer.Enabled = true; //Enables timer !!
            _timer.AutoReset = true; //Re-raise event after interval elapses. This will ensure it loops
        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_syncLock)
            {
                _resetFlag = !_resetFlag;
            }
        }

        private void StartConsumer()
        {
            _token = new CancellationTokenSource();

            _consumer = Task.Factory.StartNew(() =>
            {
                while (!_queue.IsCompleted)
                {
                    if (_token.IsCancellationRequested)
                        break;

                    Instrument item;
                    _queue.TryTake(out item);

                    if (item != null)
                    {
                        _instrumentsCache.AddOrUpdate(item.InstrumentId, new Instrument()
                        {
                            InstrumentId = item.InstrumentId,
                            AskPx = item.AskPx,
                            AskQty = item.AskQty,
                            BidPx = item.BidPx,
                            BidQty = item.BidQty,
                            Volume = item.Volume
                        });

                        var found = _deltaIndicator.Find(inst => inst.instrumentId == item.InstrumentId);
                        if (found != null)
                        {
                            found.CurrentPrice = item.AskPx;
                        }
                        else
                        {
                            _deltaIndicator.Add(new Model.DeltaIndicator(item.InstrumentId, item.AskPx, item.AskPx));
                        }
                    }
                }
            }, _token.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void PriceUpdateHandler(IPriceService sender, uint instrumentID, IPrices prices)
        {
            if (_resetFlag)
            {
                _queue.Add(new Instrument()
                {
                    InstrumentId = instrumentID,
                    AskPx = prices.AskPx,
                    AskQty = prices.AskQty,
                    BidPx = prices.BidPx,
                    BidQty = prices.BidQty,
                    Volume = prices.Volume
                });
            }
        }



        #region ICommand implementations
        private void StartPriceService(object parameter)
        {
            _producer = Task.Factory.StartNew(() =>
            {
                if (_token.Token.IsCancellationRequested)
                {
                    StopPriceService(null);
                }
                priceService.Start();
            }, _token.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            toggleButton = false;
            priceService.NewPricesArrived += PriceUpdateHandler;
        }

        private void StopPriceService(object parameter)
        {
            priceService.Stop();
            toggleButton = true;
        }

        private bool EnableStartButton(object parameter)
        {
            return toggleButton;
        }

        private bool EnableStopButton(object parameter)
        {
            return true;
        }
        #endregion
    }

    [Serializable]
    public class ObservableDictionary<TKey, TValue> :
        IDictionary<TKey, TValue>,
        INotifyPropertyChanged,
        INotifyCollectionChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private readonly Dictionary<TKey, TValue> mDictionary = new Dictionary<TKey, TValue>();
        private List<TKey> index = new List<TKey>();
        private static object locker = new object();

        public ICollection<TKey> Keys
        {
            get
            {
                return mDictionary.Keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                return mDictionary.Values;
            }
        }

        public int Count
        {
            get
            {
                return mDictionary.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                return mDictionary[key];
            }

            set
            {
                AddOrUpdate(key, value);
            }
        }

        public void AddOrUpdate(TKey key, TValue value)
        {
            TValue existing;
            if (mDictionary.TryGetValue(key, out existing))
            {
                Update(key, value);
            }
            else
            {
                Add(key, value);
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (!index.Contains(key))
                index.Add(key);

            mDictionary[key] = value;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value));
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Keys"));
            OnPropertyChanged(new PropertyChangedEventArgs("Values"));
        }

        public void Update(TKey key, TValue value)
        {
            TValue existing;

            if (mDictionary.TryGetValue(key, out existing))
            {
                int idx = index.IndexOf(key);
                mDictionary[key] = value;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, existing, idx));
                OnPropertyChanged(new PropertyChangedEventArgs("Values"));
            }
            else
            {
                Add(key, value);
            }
        }

        public void Remove(TKey key, TValue value)
        {

            mDictionary.Remove(key);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, value));
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Keys"));
            OnPropertyChanged(new PropertyChangedEventArgs("Values"));
        }



        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, args);
            }
        }

        protected void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, args);
        }

        public bool ContainsKey(TKey key)
        {
            throw new NotImplementedException();
        }

        public bool Remove(TKey key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            throw new NotImplementedException();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)mDictionary).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)mDictionary).GetEnumerator();
        }
    }
}
