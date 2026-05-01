from django.db import models
from django.contrib.auth.hashers import make_password, check_password

class User(models.Model):
    ROLE_CHOICES = [
        ('storekeeper', 'Кладовщик'),
        ('operator', 'Оператор'),
        ('manager', 'Менеджер'),
    ]
    
    login = models.CharField(max_length=100, unique=True)
    password = models.CharField(max_length=128)  
    role = models.CharField(max_length=20, choices=ROLE_CHOICES)
    full_name = models.CharField(max_length=200)
    created_at = models.DateTimeField(auto_now_add=True)
    
    def set_password(self, raw_password):
        self.password = make_password(raw_password)
    
    def check_password(self, raw_password):
        return check_password(raw_password, self.password)
    
    def __str__(self):
        return f"{self.login} ({self.get_role_display()})"