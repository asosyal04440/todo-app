# Reactive Forms:

## 1. Uygulamada gerçek sorun neydi?

Form değerleri ngModel ve ayrı component alanlarıyla tutuluyordu. Doğrulama submit
içindeki if bloklarına dağılmıştı; alanın geçerlilik, touched ve dirty bilgisi ortak bir
form tanımında bulunmuyordu.

## 2. Eklediğim parçalar

Dört form için typed FormGroup/FormControl tanımları, requiredTrimmed ve validLocalDate
validator'ları ile ortak FormFieldError component'ini ekledim.

### Validator ne demek?

Değeri inceleyip geçerliyse null, geçersizse hata anahtarı döndüren kural.
`shared/forms/form-validators.ts` içindeki requiredTrimmed, boşluklardan oluşan
adı da boş kabul eder. Angular required yalnızca boş string'i yakalar; uygulamadaki
liste/görev kuralı trim sonrası boşluğu da reddetmek zorunda.

validLocalDate, boş tarih alanına izin verir; dolu ama geçersiz tarihi reddeder.
Uzunluk sınırları önceki değişiklikte Domain'den aynaladığım feature sabitlerinden gelir.
Öncelik üç geçerli seçenekle sınırlıdır. API/Domain doğrulaması çalışmaya devam eder.

## 3. Kaldırdığım parçalar

### Değiştirdiğim kod

FormsModule/ngModel kaldırıldı; dört form ReactiveFormsModule/FormBuilder
kullanıyor. LoginPage.validateForm ve TodoForm'daki elle alan doğrulama blokları
yerini validator'lara bıraktı. Ortak iki validator ve FormFieldError eklendi.
Yeni bir kuralın yeri belirli; alan durumunu ve hatalarını yönetmek kolay.

## 4. Kodun adım adım akışı

### Ne değişti?

Önce `title = ''`, HTML'de ngModel ve submit içinde birçok if vardı.
Şimdi form, değerleriyle birlikte alanın geçerli olup olmadığını da biliyor:

```typescript
readonly form = this.builder.nonNullable.group({
  title: ['', [requiredTrimmed,
    Validators.maxLength(TODO_VALIDATION_LIMITS.todoTitleMaximumLength)]],
  description: ['', Validators.maxLength(TODO_VALIDATION_LIMITS.todoDescriptionMaximumLength)],
  dueAtLocal: ['', validLocalDate],
  priority: this.builder.nonNullable.control<TodoPriority>('Normal',
    Validators.pattern(/^(Low|Normal|High)$/)),
});
```

FormGroup bütün form; FormControl bir alan. nonNullable, boş değer için null
yerine alanın string başlangıç değerini kullanır. Böylece payload tipleri daha açık.

```html
<form [formGroup]="form" (ngSubmit)="submit()">
  <input formControlName="title" />
</form>
```

`formControlName`, input'u formdaki title kontrolüne bağlar. Artık ayrı title
alanı ile formun değerini senkron tutmuyorum.

### Touched / dirty ne?

- Touched: kullanıcı alana girip ayrıldı veya ben kontrol etmesi için işaretledim.
- Dirty: kullanıcı alanın değerini değiştirdi.
- Invalid: bir veya daha fazla kural geçemedi.

Submit'te markAllAsTouched çağırıyorum. Form invalid ise HTTP göndermiyorum.
Kullanıcı henüz hiçbir şey yapmadan bütün alanlara kırmızı hata basmıyorum.
`shared/forms/form-field-error.ts` bunları ortak kuralla gösterir.

## 5. Nerelerde uygulandı?

### Giriş ve kayıt şifresi neden farklı?

Kayıt ekranında yeni şifre politikası gerekir. Girişte kullanıcının mevcut
şifresini reddetmeden sunucuda doğrulamalıyım. LoginPage.switchMode, kayıt
modunda passwordPolicy ve zorunlu isim validator'larını ekler; girişte çıkarır.
updateValueAndValidity mevcut değerleri yeni kurallarla tekrar kontrol eder.
Şifre hatasının minimum uzunluğu da AUTH_VALIDATION_LIMITS'ten gelir.

### Hata olunca taslak kayboluyor mu?

Form yalnızca görev/list input'u değişince veya başarılı liste oluşturma
bildirimi gelince reset ediliyor. saving bayrağı değişince reset etmiyorum.
Başarısız HTTP sonrası yazdığın metin korunuyor. getRawValue ile formun
tam değerini alıp trim/UTC dönüşümünden sonra TodoRequest yayıyorum.

## 6. Ne kazandım?

Form değeri ve doğrulama aynı modelde bulunuyor. Geçersiz formu göndermiyorum; kullanıcı
hatayı ilgili alanın yanında görebiliyor. Hata sonrası taslak korunuyor.

Kaynaklar: [Reactive Forms](https://angular.dev/guide/forms/reactive-forms),
[form doğrulaması](https://angular.dev/guide/forms/form-validation).
